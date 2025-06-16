using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace YoavProject
{
    public partial class Server : Form
    {
        public static bool serverRunning;

        public TcpListener listener;

        public static List<TcpClient> allClients = new List<TcpClient>();
        
        public static Dictionary<TcpClient, int> IDUsingClients = new Dictionary<TcpClient, int>();
        public static Dictionary<int, IPEndPoint> udpEndpointsUsingID = new Dictionary<int, IPEndPoint>();

        public static Dictionary<TcpClient, string> AESkeysUsingClients = new Dictionary<TcpClient, string>();

        public static Dictionary<int, Player> playersUsingID = new Dictionary<int, Player>();
        public static Dictionary<int, string> usernameUsingID = new Dictionary<int, string>();

        private GameBoard board;
        private static WorldState state;
        private static WorldState defaultstate;

        public static int totalClients = 0;
        public static int currentlyOnline = 0;
        public const int maxQueue = 4;
        private static readonly object clientsLock = new object();
        private static readonly object stateLock = new object();

        private string RSApublic;
        private string RSAprivate;

        private bool inGame;
        private bool startingGame;
        private List<int> IDsInGameOrQueue; 
        private int round = 0;
        private Dictionary<int, int> scoresUsingID;

        private JsonHandler JsonHandler;
        public Server()
        {
            InitializeComponent();
        }

        private async void Server_Load(object sender, EventArgs e)
        {
            serverRunning = true;
            listener = new TcpListener(IPAddress.Any, StreamHelp.tcpPort);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            board = new GameBoard();

            (RSAprivate, RSApublic) = Encryption.generateRSAkeypair();
            Console.WriteLine(RSApublic);
            JsonHandler = new JsonHandler();
            IDsInGameOrQueue = new List<int>();
            scoresUsingID = new Dictionary<int, int>();
            inGame = false;
            startingGame = false;
            lock (stateLock)
            {
                defaultstate = new WorldState();
                state = new WorldState();
                defaultstate.addWorldInteractable(0, new Table(new PointF(1, 5)));
                for (int i = 1; i < 8; i++)
                {
                    Workstation a = new Workstation(new PointF(board.cols - i, board.rows - 1));
                    if (i % 3 == 0)
                        a.type = Workstation.stationType.pasta;
                    //a.onInteract += printInteract;
                    defaultstate.addWorldInteractable(i, a);

                }
            }

            UDP.denyOthers();
            _ = Task.Run(acceptClientsAsync);
            _ = Task.Run(handleClientUdpAsync);
        }

        private async Task acceptClientsAsync()
        {
            while (serverRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    NetworkStream stream = client.GetStream();

                    

                    byte[] RSApublicbytes = Convert.FromBase64String(RSApublic);
                    Console.WriteLine(RSApublicbytes);
                    Console.WriteLine(RSApublicbytes.Length);
                    byte[] RSAbytesLength = BitConverter.GetBytes(RSApublicbytes.Length);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(RSAbytesLength);

                    await stream.WriteAsync(RSAbytesLength, 0, 4);
                    await stream.WriteAsync(RSApublicbytes, 0, RSApublicbytes.Length);

                    byte[] aeskeylength = await StreamHelp.ReadExactlyAsync(stream, 4);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(aeskeylength);

                    int aesactuallength = BitConverter.ToInt32(aeskeylength, 0);
                    byte[] aeskeyenc = await StreamHelp.ReadExactlyAsync(stream, aesactuallength);

                    string aesKey = Convert.ToBase64String(Encryption.decryptRSA(aeskeyenc, RSAprivate));
                    AESkeysUsingClients.Add(client, aesKey);

                    _ = Task.Run(() => handleClientTcpAsync(client));
                    
                }
                catch (Exception e)
                {
                    if (serverRunning)
                        Console.Write("Error accepting client: " + e.Message);
                }
            }
        }

        private async Task connectClientToGame(TcpClient client, string username)
        {
            Invoke((MethodInvoker)delegate {
                label1.Text = "connecting client to game";
            });
            
            try
            {
                NetworkStream stream = client.GetStream();
                int clientId = Interlocked.Increment(ref totalClients);
                usernameUsingID.Add(clientId, username);
                List<byte> stateSyncList = new List<byte>();
                Dictionary<TcpClient, int> clientSnapshot;
                lock (clientsLock)
                {
                    currentlyOnline++;
                    clientSnapshot = new Dictionary<TcpClient, int>(IDUsingClients);
                    stateSyncList.Add((byte)Data.CompleteStateSync);
                    stateSyncList.Add((byte)playersUsingID.Count);

                    foreach (var pair in playersUsingID)
                    {
                        int id = pair.Key;
                        Player p = pair.Value;

                        //full message includes the statesync
                        byte[] playerData = UDP.createByteMessage(id, p.position.X, p.position.Y, usernameUsingID[id]);
                        stateSyncList.AddRange(playerData);
                    }

                    allClients.Add(client);
                    IDUsingClients.Add(client, clientId);
                    playersUsingID.Add(clientId, new Player(username, clientId));
                    //copy playersusingid to currplayers
                }
                lock (stateLock) //here incase i wanna copy for another place
                {
                    Console.WriteLine("Adding world data...");
                    stateSyncList.Add((byte)defaultstate.getInteractableCount());
                    stateSyncList.AddRange(defaultstate.getWorldState());
                    Console.WriteLine("Finished Adding");
                }
                byte byteId = (byte)clientId;

                //send new player joined to existing
                List<byte> newPlayerBytes = new List<byte>();
                //byte[] newPlayerBytes = new byte[1 + 1 + 4 + 4];

                newPlayerBytes.Add((byte)Data.NewPlayer);
                newPlayerBytes.AddRange(UDP.createByteMessage(clientId, 6f, 6f, username));
               
                
                foreach (TcpClient existingClient in clientSnapshot.Keys)
                {
                    Console.WriteLine("ahhhhh");
                    await StreamHelp.WriteEncrypted(existingClient.GetStream(), newPlayerBytes.ToArray(), AESkeysUsingClients[existingClient]);
                }
                Console.WriteLine($"Client connected with ID {clientId}");

                await StreamHelp.WriteEncrypted(stream, new byte[] { byteId }, AESkeysUsingClients[client]);
                Console.WriteLine("Sent id: " + byteId);

                Invoke((MethodInvoker)delegate {
                    label1.Text = stateSyncList.ToArray().Length + " ";
                });
                await StreamHelp.WriteEncrypted(stream, stateSyncList.ToArray(), AESkeysUsingClients[client]);
                Console.WriteLine("Sent stae sync list");
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Error connecting client to game: " + ex.StackTrace);
            }

        }

        private async Task handleClientUdpAsync()
        {
            Console.WriteLine("hello??");
            UdpClient receiver = new UdpClient();
            receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, UDP.regularCommunicationToServer);
            receiver.Client.Bind(endpoint);

            async Task process_data(UdpReceiveResult res)
            {
                byte[] data = res.Buffer;
                if (data.Length < 10)
                {
                    Console.WriteLine("Invalid Packet Size");
                    return;
                }

                // message type
                byte messageType = data[0];

                int clientId = (int)data[1];

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(data, 2, 4);
                    Array.Reverse(data, 6, 4);
                }

                float x = BitConverter.ToSingle(data, 2);
                float y = BitConverter.ToSingle(data, 6);
                PointF point = new PointF(x, y);

                //Console.WriteLine($"Received Type {messageType} | x: {x}, y: {y} from id: {clientId}");
                //Dictionary<int, IPEndPoint> snapshot;
                if (!GameInterval.Enabled || (GameInterval.Enabled && !IDsInGameOrQueue.Contains(clientId)))
                {
                    lock (clientsLock)
                    {
                        if (udpEndpointsUsingID.TryGetValue(clientId, out IPEndPoint existingEndpoint))
                        {
                            if (!existingEndpoint.Equals(res.RemoteEndPoint))
                            {
                                Console.WriteLine($"Warning: Mismatched endpoint for client {clientId}. Expected {existingEndpoint}, but got {res.RemoteEndPoint}.");
                            }
                        }
                        else
                        {
                            // first time seeing this clientId, add the endpoint
                            udpEndpointsUsingID[clientId] = res.RemoteEndPoint;
                            Console.WriteLine($"Registered endpoint for client {clientId}: {res.RemoteEndPoint}");
                        }

                        // update player position if found
                        if (playersUsingID.TryGetValue(clientId, out Player player))
                        {
                            player.position = point;
                        }
                        else
                        {
                            Console.WriteLine($"Player with ID {clientId} not found.");
                        }
                    }
                }
                

                //foreach (var pair in snapshot) //todo fix??
                //{
                //    int otherId = pair.Key;
                //    IPEndPoint ipEndPoint = new IPEndPoint(pair.Value.Address, UDP.regularCommunicationToClients);
                //    if (otherId == clientId) continue;

                //    try
                //    {
                //        //Console.WriteLine("Sending?");
                //        await receiver.SendAsync(data, data.Length, ipEndPoint);
                //    }
                //    catch (Exception e)
                //    {
                //        Console.WriteLine($"Failed to send to client {otherId}: {e.Message}");
                //    }
                //}

            }


            while (serverRunning)
            {
                try
                {
                    UdpReceiveResult result = await receiver.ReceiveAsync();
                    await process_data(result);
                }
                catch (Exception e)
                {
                    Console.WriteLine("UDP Receive error: " + e.Message);
                }

            }

            receiver.Close();
        }

        private async Task broadcastGameState()
        {
            using (UdpClient sender = new UdpClient())
            {
                sender.EnableBroadcast = true;

                Dictionary<int, Player> playerSnapshot;

                lock (clientsLock)
                {
                    playerSnapshot = new Dictionary<int, Player>(playersUsingID);
                }

                int playerCount = playerSnapshot.Count;
                List<byte> data = new List<byte>();

                data.Add((byte)Data.PositionStateSync);
                data.Add((byte)playerCount);



                foreach (var pair in playerSnapshot)
                {
                    byte[] playerData = UDP.createByteMessage(pair.Key, pair.Value.position.X, pair.Value.position.Y);
                    data.AddRange(playerData);
                }

                try
                {
                    IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, UDP.regularCommunicationToClients);
                    await sender.SendAsync(data.ToArray(), data.ToArray().Length, broadcastEP);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Broadcast failed: {e.Message}");
                }
            }
        }

        private async Task handleClientTcpAsync(TcpClient client)
        {
            bool isSignedIn = false;
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];

                    while (serverRunning && client.Connected)
                    {
                        byte[] databytebytes = await StreamHelp.ReadEncrypted(stream, AESkeysUsingClients[client]);
                        
                        if (databytebytes[0] == -1)
                        {
                            Console.WriteLine("Disconnected Client[TCP].");
                            break;
                        }
                        #region signin part
                        if (!isSignedIn)
                        {
                            byte[] usernamelengthbytes = new byte[4];
                            Array.Copy(databytebytes, 1, usernamelengthbytes, 0, 4);
                            if (!BitConverter.IsLittleEndian)
                                Array.Reverse(usernamelengthbytes);

                            int usernamelength = BitConverter.ToInt32(usernamelengthbytes, 0);
                            string username = Encoding.UTF8.GetString(databytebytes, 5, usernamelength);
                            Console.WriteLine("username: " + username);
                            string password = Encoding.UTF8.GetString(databytebytes, 5 + usernamelength, databytebytes.Length - usernamelength - 5);

                            byte[] reply = new byte[1];
                            if (RegisterLogin.isFieldValid(username))
                            {
                                switch ((Registration)databytebytes[0])
                                {
                                    case Registration.Register:
                                        Console.WriteLine("register");
                                        if (JsonHandler.userExists(username))
                                            reply[0] = (byte)Registration.ErrorTaken;
                                        else
                                        {
                                            if (RegisterLogin.isFieldValid(password))
                                            {
                                                JsonHandler.addUser(username, password);
                                                reply[0] = (byte)Registration.RegisterSuccess;
                                            }
                                            else
                                            {
                                                reply[0] = (byte)Registration.ErrorInvalid;
                                            }
                                        }
                                        break;

                                    case Registration.Login:
                                        if (JsonHandler.userExists(username))
                                        {
                                            if (JsonHandler.userLoggedIn(username))
                                                stream.WriteByte((byte)Registration.ErrorLoggedIn);
                                            else
                                            {
                                                if (RegisterLogin.isFieldValid(password))
                                                {
                                                    if (JsonHandler.verifyLogin(username, password))
                                                    {
                                                        
                                                        reply[0] = (byte)Registration.LoginSuccess;
                                                        isSignedIn = true;

                                                        await StreamHelp.WriteEncrypted(stream, reply, AESkeysUsingClients[client]);
                                                        await Task.Run(() => connectClientToGame(client, username));
                                                        //possibly return here
                                                    }
                                                    else
                                                    {
                                                        reply[0] = (byte)Registration.ErrorWrong;
                                                    }
                                                }
                                                else
                                                {
                                                    reply[0] = (byte)Registration.ErrorInvalid;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            reply[0] = (byte)Registration.ErrorWrong;
                                        }

                                        break;

                                }
                            }
                            else
                            {
                                reply[0] = (byte)Registration.ErrorInvalid;
                            }
                            Console.WriteLine("reply: " + reply[0]);
                            if (!isSignedIn)
                                await StreamHelp.WriteEncrypted(stream, reply, AESkeysUsingClients[client]);
                        }
                        #endregion
                        else
                        {
                            bool flag = false;
                            switch ((Data)databytebytes[0])
                            {
                                case Data.ObjInteract:
                                    //buffer = await StreamHelp.ReadEncrypted(stream, AESkeysUsingClients[client]); //FIX HERE
                                    int clientId = IDUsingClients[client];
                                    int interactableId = (int)databytebytes[1];
                                    byte[] successinteraction = new byte[2];
                                    flag = false;
                                    lock (stateLock)
                                    lock (clientsLock)
                                    {
                                        if (IDsInGameOrQueue.Contains(clientId) && inGame)
                                        {
                                            if (state.getInteractableObject(interactableId) is Table)
                                            {
                                                if (playersUsingID[clientId].platesHeld < 3)
                                                {
                                                    if (state.interactWith(interactableId))
                                                    {
                                                            playersUsingID[clientId].addPlate();
                                                            flag = true;
                                                    }
                                                }
                                            }
                                            else if (state.getInteractableObject(interactableId) is Workstation)
                                            {
                                                if (playersUsingID[clientId].platesHeld > 0)
                                                {
                                                    if (state.interactWith(interactableId))
                                                    {
                                                            playersUsingID[clientId].removePlate();
                                                            flag = true;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (defaultstate.getInteractableObject(interactableId) is Table)
                                            {
                                                Console.WriteLine("aa");
                                                if (playersUsingID[clientId].platesHeld < 3)
                                                {
                                                    Console.WriteLine("ab");
                                                    if (defaultstate.interactWith(interactableId))
                                                    {
                                                        playersUsingID[clientId].addPlate();
                                                        Console.WriteLine("ac");
                                                        flag = true;
                                                    }
                                                }
                                            }
                                            else if (defaultstate.getInteractableObject(interactableId) is Workstation)
                                            {
                                                if (playersUsingID[clientId].platesHeld > 0)
                                                {
                                                    if (defaultstate.interactWith(interactableId))
                                                    {
                                                            playersUsingID[clientId].removePlate();
                                                            flag = true;
                                                    }
                                                }
                                            }
                                        }

                                    }
                                    if (flag)
                                    {
                                        Console.WriteLine("ad");
                                        successinteraction[0] = (byte)Data.ObjInteractSuccess;
                                        successinteraction[1] = (byte)interactableId;
                                        Console.WriteLine("ae");
                                        TcpClient[] clientsinqueue;
                                        TcpClient[] clients;
                                        Dictionary<TcpClient, string> aeskeys;
                                        bool flag2 = false;
                                        lock (clientsLock)
                                        {
                                            clients = IDUsingClients.Where(kvp => !IDsInGameOrQueue.Contains(kvp.Value)).Select(kvp => kvp.Key).ToArray();
                                            aeskeys = new Dictionary<TcpClient, string>(AESkeysUsingClients);
                                            clientsinqueue = IDUsingClients.Where(kvp => IDsInGameOrQueue.Contains(kvp.Value)).Select(kvp => kvp.Key).ToArray();
                                            if (IDsInGameOrQueue.Contains(clientId) && inGame)
                                            {
                                                flag2 = true;
                                            }
                                        }
                                        if (flag2)
                                        {
                                            await StreamHelp.WriteEncryptedToAll(clientsinqueue, successinteraction, aeskeys);
                                        } else
                                        {
                                            await StreamHelp.WriteEncryptedToAll(clients, successinteraction, aeskeys);
                                        }
                                    }
                                    break;
                                case Data.EnterQueue:
                                    Console.WriteLine("Received EnterQueue");
                                    if (!inGame)
                                    {
                                        Console.WriteLine("Not in game");
                                        flag = false;
                                        int nowInQueue = 0;
                                        lock (clientsLock)
                                        {
                                            if (!IDsInGameOrQueue.Contains(IDUsingClients[client]))
                                            {
                                                Console.WriteLine("player id not in queue");
                                                if (IDsInGameOrQueue.Count <= maxQueue)
                                                {
                                                    Console.WriteLine("adding player");
                                                    IDsInGameOrQueue.Add(IDUsingClients[client]);
                                                    nowInQueue = IDsInGameOrQueue.Count;
                                                    flag = true;
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("removing player");
                                                IDsInGameOrQueue.Remove(IDUsingClients[client]);
                                                nowInQueue = IDsInGameOrQueue.Count;
                                                flag = true;
                                            }
                                        }
                                        if (flag)
                                        {
                                            Console.WriteLine("sending message of enterqueue");
                                            byte[] message = new byte[2];
                                            message[0] = (byte)Data.EnterQueue;
                                            message[1] = (byte)IDUsingClients[client];
                                            Invoke((MethodInvoker)delegate {
                                                label1.Text = IDUsingClients[client]+"";
                                            });

                                            await StreamHelp.WriteEncryptedToAll(IDUsingClients.Keys.ToArray(), message, AESkeysUsingClients);
                                        }
                                        Console.WriteLine((nowInQueue == maxQueue) + " " + (nowInQueue == currentlyOnline) + " " + nowInQueue + " " + currentlyOnline);
                                        checkIfGameCanStart(nowInQueue);
                                        
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                // Likely a disconnection or network error
                Console.WriteLine("IO Exception (client likely disconnected): " + ioEx.Message);
            }
            catch (SocketException sockEx)
            {
                Console.WriteLine("Socket exception (client likely disconnected): " + sockEx.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Cleaning up client connection.");
                client.Close();
                int nowInQueue = 0;
                lock (clientsLock)
                {
                    udpEndpointsUsingID.Remove(IDUsingClients[client]);
                    AESkeysUsingClients.Remove(client);
                    playersUsingID.Remove(IDUsingClients[client]);
                    allClients.Remove(client);
                    JsonHandler.disconnectUser(usernameUsingID[IDUsingClients[client]]);
                    currentlyOnline--;
                    if (IDsInGameOrQueue.Contains(IDUsingClients[client]))
                    {
                        IDsInGameOrQueue.Remove(IDUsingClients[client]);
                        nowInQueue = IDsInGameOrQueue.Count;
                        checkIfGameCanStart(nowInQueue);
                    }
                    IDUsingClients.Remove(client);
                }
                
            }
        }

        private async void checkIfGameCanStart(int nowInQueue)
        {
            if (nowInQueue == maxQueue || nowInQueue == currentlyOnline)
            {
                Console.WriteLine("sending message of start countdown");
                byte[] message = new byte[1];
                message[0] = (byte)Data.CountdownStart;
                startingGame = true;
                Invoke((MethodInvoker)delegate
                {
                    GameCountdown.Start();
                });

                await StreamHelp.WriteEncryptedToAll(IDUsingClients.Keys.ToArray(), message, AESkeysUsingClients);
            }
            if (startingGame && nowInQueue < maxQueue && nowInQueue != currentlyOnline)
            {
                Console.WriteLine("sending message of stop countdown");
                byte[] message = new byte[1];
                message[0] = (byte)Data.CountdownStop;
                startingGame = false;
                Invoke((MethodInvoker)delegate
                {
                    GameCountdown.Stop();
                });

                await StreamHelp.WriteEncryptedToAll(IDUsingClients.Keys.ToArray(), message, AESkeysUsingClients);
            }
        }

        private async void GameLoop_Tick(object sender, EventArgs e)
        {
            await broadcastGameState();
        }

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            serverRunning = false;
            listener.Stop();
            lock (clientsLock)
            {
                foreach (var client in allClients)
                    client.Close();
            }
        }

        private async void GameCountdown_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("sending game start");
            TcpClient[] clients;
            Dictionary<TcpClient, string> aeskeys;
            lock (clientsLock)
            {
                clients = IDUsingClients.Keys.ToArray();
                aeskeys = new Dictionary<TcpClient, string>(AESkeysUsingClients);
            }
            Invoke((MethodInvoker)delegate
            {
                GameCountdown.Stop();
            });
            startingGame = false;
            inGame = true;
            byte[] message = new byte[1];
            message[0] = (byte)Data.GameStart;
            await StreamHelp.WriteEncryptedToAll(clients, message, aeskeys);
            List<byte> data = new List<byte>();
            data.Add((byte)Data.WorldStateSyncGame);
            lock (stateLock)
            {
                state.setUpForGameMap();
                data.Add((byte)state.getInteractableCount());
                data.AddRange(state.getWorldState());
            }
            var startPositions = new PointF[]
            {
                new PointF(1, 3),
                new PointF(9, 9),
                new PointF(9, 3),
                new PointF(1, 9)
            };
            TcpClient[] clientsinqueue;
            int count= 0;
            lock (clientsLock)
            {
                
                clientsinqueue = IDUsingClients.Where(kvp => IDsInGameOrQueue.Contains(kvp.Value)).Select(kvp => kvp.Key).ToArray();
                for (int i = 0; i < IDsInGameOrQueue.Count && i < startPositions.Length; i++)
                {
                    scoresUsingID.Add(IDsInGameOrQueue[i], 0);
                    playersUsingID[IDsInGameOrQueue[i]].position = startPositions[i];
                }
                count = IDsInGameOrQueue.Count;
            }
            byte[] interval = new byte[1];
            interval[0] = (byte)Data.Interval;
            List<byte> playerpos = new List<byte>();
            playerpos.Add((byte)Data.Position);
            for (int i = 0; i < count; i++)
            {
                byte[] xpos = BitConverter.GetBytes(startPositions[i].X);
                byte[] ypos = BitConverter.GetBytes(startPositions[i].Y);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(xpos);
                    Array.Reverse(ypos);
                }
                playerpos.AddRange(xpos);
                playerpos.AddRange(ypos);
                await StreamHelp.WriteEncrypted(clientsinqueue[i].GetStream(), playerpos.ToArray(), aeskeys[clientsinqueue[i]]);
                playerpos.Clear();
                playerpos.Add((byte)Data.Position);
            }
            
            await StreamHelp.WriteEncryptedToAll(clientsinqueue, interval, aeskeys);
            await StreamHelp.WriteEncryptedToAll(clientsinqueue, data.ToArray(), aeskeys);
            Invoke((MethodInvoker)delegate
            {
                GameInterval.Start();
            });

        }

        private async void GameInterval_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                GameInterval.Stop();
                GameRound.Start();
            });
            TcpClient[] clientsinqueue;
            Dictionary<TcpClient, string> aeskeys;
            lock (clientsLock)
            {
                clientsinqueue = IDUsingClients.Where(kvp => IDsInGameOrQueue.Contains(kvp.Value)).Select(kvp => kvp.Key).ToArray();
                aeskeys = new Dictionary<TcpClient, string>(AESkeysUsingClients);
            }
            List<byte> data = new List<byte>();
            data.Add((byte)Data.WorldStateSyncGame);
            lock (stateLock)
            {
                state.setUpForGameMap();
                MapParser.loadMapRandom(state);
                data.Add((byte)state.getInteractableCount());
                data.AddRange(state.getWorldState());
            }
            byte[] interval = new byte[1];
            interval[0] = (byte)Data.Interval;
            round++;
            await StreamHelp.WriteEncryptedToAll(clientsinqueue, data.ToArray(), aeskeys);
            await StreamHelp.WriteEncryptedToAll(clientsinqueue, interval, aeskeys);
        }
        
        private async void GameRound_Tick(object sender, EventArgs e)
        {
            if (round <= 7)
            {
                Dictionary<int, int> workstationPlatesID = new Dictionary<int, int>();
                Dictionary<int, int> newScores;
                lock (stateLock)
                {
                    lock (clientsLock)
                    {
                        for (int i = 1; i <= IDsInGameOrQueue.Count; i++)
                        {
                            Workstation a = (Workstation)state.getInteractableObject(i);
                            workstationPlatesID.Add(i, a.getPlates());
                        }

                        var topTwo = workstationPlatesID.OrderByDescending(pair => pair.Value).Take(2).ToArray();
                        if (topTwo.Length > 0 && scoresUsingID.ContainsKey(topTwo[0].Key))
                        {
                            scoresUsingID[topTwo[0].Key] += 2;
                        }

                        if (topTwo.Length > 1 && scoresUsingID.ContainsKey(topTwo[1].Key))
                        {
                            scoresUsingID[topTwo[1].Key] += 1;
                        }
                        newScores = new Dictionary<int, int>(scoresUsingID);
                    }
                }

                Invoke((MethodInvoker)delegate
                {
                    GameInterval.Start();
                    GameRound.Stop();
                });
                var startPositions = new PointF[]
                {
                new PointF(1, 3),
                new PointF(9, 9),
                new PointF(9, 3),
                new PointF(1, 9)
                };

                List<byte> data = new List<byte>();
                data.Add((byte)Data.WorldStateSyncGame);
                lock (stateLock)
                {
                    state.setUpForGameMap();
                    data.Add((byte)state.getInteractableCount());
                    data.AddRange(state.getWorldState());
                }
                
                TcpClient[] clientsinqueue;
                int count = 0;
                Dictionary<TcpClient, string> aeskeys;
                lock (clientsLock)
                {
                    aeskeys = new Dictionary<TcpClient, string>(AESkeysUsingClients);
                    clientsinqueue = IDUsingClients.Where(kvp => IDsInGameOrQueue.Contains(kvp.Value)).Select(kvp => kvp.Key).ToArray();
                    for (int i = 0; i < IDsInGameOrQueue.Count && i < startPositions.Length; i++)
                    {
                        playersUsingID[IDsInGameOrQueue[i]].position = startPositions[i];
                    }
                    count = IDsInGameOrQueue.Count;
                }
                List<byte> scores = new List<byte>();
                scores.Add((byte)Data.Score);
                scores.Add((byte)newScores.Count);
                foreach (var pair in newScores)
                {
                    scores.Add((byte)pair.Key);
                    scores.Add((byte)pair.Value);
                }
                await StreamHelp.WriteEncryptedToAll(clientsinqueue, scores.ToArray(), aeskeys);

                byte[] interval = new byte[1];
                interval[0] = (byte)Data.Interval;
                List<byte> playerpos = new List<byte>();
                playerpos.Add((byte)Data.Position);
                for (int i = 0; i < count; i++)
                {
                    byte[] xpos = BitConverter.GetBytes(startPositions[i].X);
                    byte[] ypos = BitConverter.GetBytes(startPositions[i].Y);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(xpos);
                        Array.Reverse(ypos);
                    }
                    playerpos.AddRange(xpos);
                    playerpos.AddRange(ypos);
                    await StreamHelp.WriteEncrypted(clientsinqueue[i].GetStream(), playerpos.ToArray(), aeskeys[clientsinqueue[i]]);
                    playerpos.Clear();
                    playerpos.Add((byte)Data.Position);
                }

                await StreamHelp.WriteEncryptedToAll(clientsinqueue, interval, aeskeys);
                await StreamHelp.WriteEncryptedToAll(clientsinqueue, data.ToArray(), aeskeys);
                
            }
            else
            {
                Invoke((MethodInvoker)delegate
                {
                    GameInterval.Stop();
                    GameRound.Stop();
                });

                List<byte> data = new List<byte>();
                data.Add((byte)Data.WorldStateSyncGame);
                lock (stateLock)
                {
                    state.setUpForGameMap();
                    data.Add((byte)defaultstate.getInteractableCount());
                    data.AddRange(defaultstate.getWorldState());
                }

                TcpClient[] clientsinqueue;
                int count = 0;
                Dictionary<TcpClient, string> aeskeys;
                lock (clientsLock)
                {
                    aeskeys = new Dictionary<TcpClient, string>(AESkeysUsingClients);
                    clientsinqueue = IDUsingClients.Where(kvp => IDsInGameOrQueue.Contains(kvp.Value)).Select(kvp => kvp.Key).ToArray();
                }
                byte[] stop = new byte[1];
                stop[0] = (byte)Data.GameStop;

                lock (clientsLock)
                {
                    IDsInGameOrQueue.Clear();
                    scoresUsingID.Clear();
                }

                inGame = false;
                round = 0;
                await StreamHelp.WriteEncryptedToAll(clientsinqueue, stop, aeskeys);
                await StreamHelp.WriteEncryptedToAll(clientsinqueue, data.ToArray(), aeskeys);
            }
            
        }
    }
}
