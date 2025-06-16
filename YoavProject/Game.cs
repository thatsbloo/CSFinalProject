using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoavProject
{
    public partial class Game : Form
    {

        private List<Player> online_players;
        //private Player self;
        private GameBoard board;
        private RegisterLogin login;

        private Image backgroundSpriteSheet;

        private HashSet<Keys> pressedKeys;

        public static bool isDebugMode { get; private set; }

        public static volatile bool connected;
        public static bool signup { get; private set; }

        private static readonly object playersLock = new object();
        private static readonly object stateLock = new object();

        private string RSApublicKey;

        private static string AESkey;
        public static volatile bool isQueue = false;
        public static volatile bool isGame = false;
        public static HashSet<int> IdInGameOrQueue = new HashSet<int>();
        public static volatile bool canMove = true;
        public static Dictionary<int, int> ScoresUsingID = new Dictionary<int, int>();

        private string username = "";

        public static UdpClient UDPclient;
        public Game()
        {
            InitializeComponent();


            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            
            this.DoubleBuffered = true;

            isDebugMode = false;

            connected = false;
            signup = false;

            //self = new Player();
            online_players = new List<Player>();
            board = new GameBoard();

            login = new RegisterLogin();
            
            login.loginPressed += handleLogin;
            login.registerPressed += handleRegister;

            pressedKeys = new HashSet<Keys>();

        }

        private static TcpClient tcpClient;
        public static int clientId { get; private set; }

        
        private async void Game_Load(object sender, EventArgs e)
        {
            //Controls.Add(board);
            Controls.Add(login);
            this.ClientSize = new Size(64 * board.cols, 64 * board.rows);
            login.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            //Controls.Add(self);

            
            UDP.serverDoesntExist();
            tcpClient = new TcpClient(UDP.serverAddress.ToString(), StreamHelp.tcpPort);
            NetworkStream stream = tcpClient.GetStream();
            UDPclient = new UdpClient();

            byte[] rsakeylength = await StreamHelp.ReadExactlyAsync(stream, 4);
            Console.WriteLine("hey");
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(rsakeylength);
            Console.WriteLine("hey2");
            int rsaactuallength = BitConverter.ToInt32(rsakeylength, 0);
            byte[] RSApublic = await StreamHelp.ReadExactlyAsync(stream, rsaactuallength);
            Console.WriteLine("hey3");
            RSApublicKey = Convert.ToBase64String(RSApublic);
            Console.WriteLine("hey4");
            AESkey = Encryption.generateAESkey();

            byte[] AESkeyEncrypted = Encryption.encryptRSA(Convert.FromBase64String(AESkey), RSApublicKey);
            byte[] AESlengthPrefix = BitConverter.GetBytes(AESkeyEncrypted.Length);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(AESlengthPrefix);

            await stream.WriteAsync(AESlengthPrefix, 0, 4);
            await stream.WriteAsync(AESkeyEncrypted, 0, AESkeyEncrypted.Length);
            signup = true;

            await Task.Run(handleServerLoginReplies);


        }

        private async Task joinTheGameWorld()
        {
            
            Console.WriteLine("starting join the gameworld?");
            NetworkStream stream = tcpClient.GetStream();
            // read byte for client id
            byte[] decryptedID = await StreamHelp.ReadEncrypted(stream, AESkey);
            clientId = (int)decryptedID[0];
            board.setSelf(this.username, clientId);
            Console.WriteLine("my id: " + clientId);

            byte[] buffer = await StreamHelp.ReadEncrypted(stream, AESkey);
            Console.WriteLine("buffer sync length: " + buffer.Length);
            
            byte packetType = buffer[0];

            Console.WriteLine(packetType);
            if (packetType == (byte)Data.CompleteStateSync) // state sync
            {
                int playerCount = buffer[1];
                int offset = 2;

                //byte[] playersBuffer = Array.Copy(); // each player: 1 byte data.position + 1 byte id + + pos 4 float X + 4 float Y = 10 bytes playerCount * 10

                for (int i = 0; i < playerCount; i++)
                {
                    byte id = buffer[offset];
                    offset += 1;

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(buffer, offset, 4); // posX
                        Array.Reverse(buffer, offset + 4, 4); // posY
                        Array.Reverse(buffer, offset + 8, 4);
                    }

                    float x = BitConverter.ToSingle(buffer, offset);
                    float y = BitConverter.ToSingle(buffer, offset + 4);
                    int usernamelength = BitConverter.ToInt32(buffer, offset + 8);
                    offset += 12;

                    string username = Encoding.UTF8.GetString(buffer, offset, usernamelength);
                    offset += usernamelength;

                    Player p = new Player(username, id);
                    p.position = new PointF(x, y);

                    lock (playersLock)
                    {
                        GameBoard.onlinePlayers[id] = p;
                        online_players.Add(p);
                    }

                    Console.WriteLine($"Synced Player: {username}, ID: {id}, Pos: ({x}, {y})");
                }

                syncWorldState(buffer, offset);
            }
            connected = true;
            _ = Task.Run(listenForUdpUpdatesAsync);
            _ = Task.Run(listenForTcpUpdatesAsync);
        }

        private async Task handleServerLoginReplies()
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (signup)
                {
                    byte[] msgtype = await StreamHelp.ReadEncrypted(stream, AESkey);

                    if (msgtype[0] == -1)
                    {
                        Console.WriteLine("Disconnected [TCP].");
                        break;
                    }

                    switch ((Registration)msgtype[0])
                    {
                        case Registration.ErrorTaken:
                            login.displayErrorMessage("Username already taken!");
                            break;
                        case Registration.ErrorWrong:
                            login.displayErrorMessage("Incorrect username or password!");
                            break;
                        case Registration.ErrorInvalid:
                            login.displayErrorMessage("Invalid username or password!");
                            break;
                        case Registration.ErrorLoggedIn:
                            login.displayErrorMessage("This user is already logged in!");
                            break;
                        case Registration.RegisterSuccess:
                            login.displaySuccessMessage("Registration successful!");
                            break;
                        case Registration.LoginSuccess:
                            Invoke((MethodInvoker)delegate {
                                Controls.Remove(login);
                                login.Dispose();
                                Controls.Add(board);
                                board.BringToFront();
                                board.Show();
                                signup = false;
                            });
                            Console.WriteLine("await jointhegameworld");
                            await joinTheGameWorld();
                            break;
                        default:
                            break;

                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }
        
        private async void handleLogin()
        {
            if (!login.areFieldsValid())
            {
                login.displayErrorMessage("Username and Password must only include English letters and numbers!");
            }
            Stream stream = tcpClient.GetStream();
            List<byte> data = new List<byte>();
            //byte[] msgtype = new byte[] { (byte)Registration.Login };
            //await StreamHelp.WriteEncrypted(stream, msgtype, AESkey);

            
            string username = login.getUsername();
            this.username = username;
            string pass = login.getPassword();

            byte[] usernameLengthBytes = BitConverter.GetBytes(username.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(usernameLengthBytes);
            }
            data.Add((byte)Registration.Login);
            data.AddRange(usernameLengthBytes);
            data.AddRange(Encoding.UTF8.GetBytes(username));
            data.AddRange(Encoding.UTF8.GetBytes(pass));

            await StreamHelp.WriteEncrypted(stream, data.ToArray(), AESkey);
        }

        private async void handleRegister()
        {
            if (!login.areFieldsValid())
            {
                login.displayErrorMessage("Username and Password must only include English letters and numbers!");
            }
            Stream stream = tcpClient.GetStream();
            List<byte> data = new List<byte>();
            //byte[] msgtype = new byte[] { (byte)Registration.Login };
            //await StreamHelp.WriteEncrypted(stream, msgtype, AESkey);


            string username = login.getUsername();
            string pass = login.getPassword();

            byte[] usernameLengthBytes = BitConverter.GetBytes(username.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(usernameLengthBytes);
            }
            data.Add((byte)Registration.Register);
            data.AddRange(usernameLengthBytes);
            data.AddRange(Encoding.UTF8.GetBytes(username));
            data.AddRange(Encoding.UTF8.GetBytes(pass));
            Console.WriteLine(AESkey);
            await StreamHelp.WriteEncrypted(stream, data.ToArray(), AESkey);
        }

        private async Task listenForUdpUpdatesAsync()
        {
            UdpClient listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, UDP.regularCommunicationToClients);
            listener.Client.Bind(serverEP);
            while (connected)
            {
                try
                {
                    UdpReceiveResult res = await listener.ReceiveAsync();
                    if (res.RemoteEndPoint.Address.Equals(UDP.serverAddress))
                        handleUdpUpdate(res);
                }
                catch (Exception e)
                {
                    Console.WriteLine("UDP receive error: " + e.Message);
                }
            }

            listener.Close();
        }

        private void handleUdpUpdate(UdpReceiveResult res)
        {
            byte[] data = res.Buffer;
            //if (data.Length != 10)
            //{
            //    Console.WriteLine("Invalid packet size");
            //    return;
            //}

            byte messageType = data[0];
            byte playerCount = data[1];

            if (messageType == (byte)Data.PositionStateSync)
            {
                for (int i = 0; i < playerCount; i++)
                {
                    int offset = i * 9;
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(data, 3+offset, 4);
                        Array.Reverse(data, 7+offset, 4);
                    }

                    float playerx = BitConverter.ToSingle(data, 3+offset);
                    float playery = BitConverter.ToSingle(data, 7+offset);
                    PointF playerposition = new PointF(playerx, playery);

                    if (GameBoard.onlinePlayers.TryGetValue(data[2 + offset], out Player player))
                    {
                        player.position = playerposition;
                    }
                    else
                    {
                        //Console.WriteLine($"Received update for unknown player {data[2+offset]}");
                    }
                }
                
            }
        }

        private async Task listenForTcpUpdatesAsync() //finish fixing
        {
            NetworkStream stream = tcpClient.GetStream();
            //byte[] buffer = new byte[1024];

            try
            {
                while(connected)
                {
                    byte[] databytebytes = await StreamHelp.ReadEncrypted(stream, AESkey);

                    if (databytebytes[0] == -1)
                    {
                        Console.WriteLine("Disconnected [TCP].");
                        break;
                    }
                    byte[] res;
                    Console.WriteLine(databytebytes[0]);
                    switch((Data)databytebytes[0])
                    {
                       
                        case Data.NewPlayer:
                            //res = await StreamHelp.ReadExactlyAsync(stream, 9);

                            byte playerId = databytebytes[1];
                            if (!BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(databytebytes, 2, 4);
                                Array.Reverse(databytebytes, 6, 4);
                                Array.Reverse(databytebytes, 10, 4);
                            }
                            float posX = BitConverter.ToSingle(databytebytes, 2);
                            float posY = BitConverter.ToSingle(databytebytes, 6);

                            int usernamelength = BitConverter.ToInt32(databytebytes, 10);
                            string newPlayerUsername = Encoding.UTF8.GetString(databytebytes, 14, usernamelength);
                            Console.WriteLine($"New player joined! ID: {playerId}");

                            Player p = new Player(newPlayerUsername, playerId);
                            p.position = new PointF(posX, posY);
                            
                            lock (playersLock)
                            {
                                GameBoard.onlinePlayers.Add(playerId, p);
                                online_players.Add(p);
                            }
                            break;
                        case Data.ObjInteractSuccess:
                            //res = await StreamHelp.ReadExactlyAsync(stream, 2);
                            lock (stateLock)
                            {
                                int interactableId = (int)databytebytes[1];
                                if (board.state.getInteractableObject(interactableId) is Table)
                                {
                                    board.player.addPlate();
                                    board.state.interactWith(interactableId);
                                }
                                else if (board.state.getInteractableObject(interactableId) is Workstation)
                                {
                                    board.player.removePlate();
                                    board.state.interactWith(interactableId);
                                }
                            }
                            

                            break;
                        case Data.EnterQueue:
                            Console.WriteLine("Emterqueue");
                            if (databytebytes[1] == (byte)clientId)
                                isQueue = !isQueue;
                            IdInGameOrQueue.Add(databytebytes[1]);
                            Console.WriteLine(databytebytes[1] + " " + isQueue);
                            break;
                        case Data.CountdownStart:
                            if (isQueue)
                            {
                                Invoke((MethodInvoker)delegate
                                {
                                    board.countdownNum = 5;
                                    Console.WriteLine("hey!!");
                                    GameCountdown.Start();
                                });
                            }
                            break;
                        case Data.CountdownStop:
                            Invoke((MethodInvoker)delegate
                            {
                                GameCountdown.Stop();
                                board.countdownNum = 6;
                            });
                            break;
                        case Data.GameStart:
                            if (isQueue)
                            {
                                Invoke((MethodInvoker)delegate
                                {
                                    GameCountdown.Stop();
                                    board.countdownNum = 6;
                                });
                                foreach (int id in IdInGameOrQueue)
                                {
                                    ScoresUsingID[id] = 0;
                                }
                                isQueue = false;
                                isGame = true;
                            }
                            break;
                        case Data.WorldStateSyncGame:
                            if (isGame)
                            {
                                lock (stateLock)
                                {
                                    syncWorldState(databytebytes, 1);
                                }
                            }
                            break;
                        case Data.Interval:
                            canMove = !canMove;
                            break;
                        case Data.Position:
                            if (!BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(databytebytes, 1, 4);
                                Array.Reverse(databytebytes, 5, 4);
                            }
                            board.player.position = new PointF(BitConverter.ToSingle(databytebytes, 1), BitConverter.ToSingle(databytebytes, 5));
                            break;
                        case Data.Score:
                            int scorecount = (int)databytebytes[1];
                            for (int i = 0; i < scorecount; i++)
                            {
                                int id = (int)databytebytes[2+i];
                                int score = (int)databytebytes[3+i];
                                ScoresUsingID[id] = score;
                            }
                            break;
                        case Data.GameStop:
                            ScoresUsingID.Clear();
                            IdInGameOrQueue.Clear();
                            isGame = false;
                            break;
                        default:
                            break;

                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

        private async void syncWorldState(byte[] buffer, int startIndex = 0)
        {
            board.state.clearWorldMap();
            int interactablecount = buffer[startIndex];
            Console.WriteLine(interactablecount);
            int stateoffset = 0;
            for (int i = 0; i < interactablecount; i++)
            {
                int id = (int)buffer[1 + startIndex + stateoffset];
                byte type = buffer[2 + startIndex + stateoffset];
                Console.WriteLine($"type: {type} id: {id}");
                switch ((InteractableObject.Types)type)
                {
                    case InteractableObject.Types.Table:
                        byte[] tbytes = new byte[17];
                        Array.Copy(buffer, 3 + startIndex + stateoffset, tbytes, 0, tbytes.Length);
                        stateoffset += 2 + tbytes.Length;
                        int platesOnTable = (int)tbytes[0];
                        byte[] tposx = new byte[4];
                        byte[] tposy = new byte[4];
                        byte[] tsizew = new byte[4];
                        byte[] tsizeh = new byte[4];
                        Array.Copy(tbytes, 1, tposx, 0, 4);
                        Array.Copy(tbytes, 5, tposy, 0, 4);
                        Array.Copy(tbytes, 9, tsizew, 0, 4);
                        Array.Copy(tbytes, 13, tsizeh, 0, 4);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(tposx);
                            Array.Reverse(tposy);
                            Array.Reverse(tsizew);
                            Array.Reverse(tsizeh);
                        }

                        PointF tpos = new PointF(BitConverter.ToSingle(tposx, 0), BitConverter.ToSingle(tposy, 0));
                        SizeF tsize = new SizeF(BitConverter.ToSingle(tsizew, 0), BitConverter.ToSingle(tsizeh, 0));

                        lock (stateLock)
                            board.state.addWorldInteractable(id, new Table(tpos, tsize, platesOnTable));
                        break;
                    case InteractableObject.Types.Workstation:
                        byte[] wbytes = new byte[9];
                        Array.Copy(buffer, 3 + startIndex + stateoffset, wbytes, 0, wbytes.Length);
                        stateoffset += 2 + wbytes.Length;
                        Workstation.stationType stationType = (Workstation.stationType)wbytes[0];
                        byte[] wposx = new byte[4];
                        byte[] wposy = new byte[4];
                        byte[] wsizew = new byte[4];
                        byte[] wsizeh = new byte[4];
                        Array.Copy(wbytes, 1, wposx, 0, 4);
                        Array.Copy(wbytes, 5, wposy, 0, 4);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(wposx);
                            Array.Reverse(wposy);
                        }

                        PointF wpos = new PointF(BitConverter.ToSingle(wposx, 0), BitConverter.ToSingle(wposy, 0));
                        lock (stateLock)
                            board.state.addWorldInteractable(id, new Workstation(wpos, stationType));
                        break;
                    default:
                        break;
                }
            }
        }

        private void Game_Resize(object sender, EventArgs e)
        {
            //board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            
            //board.Focus();
            board.Update();
        }

        private void Game_FormClosing(object sender, FormClosingEventArgs e)
        {
            connected = false;
            tcpClient.Close();
        }

        public static async void sendMessage(byte[] message)
        {
            await StreamHelp.WriteEncrypted(tcpClient.GetStream(), message, AESkey);
        }

        private void GameCountdown_Tick(object sender, EventArgs e)
        {
            Console.WriteLine(board.countdownNum);
            board.countdownNum--;
        }
    }
}
