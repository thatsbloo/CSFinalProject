using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private GameBoard board;
        private static WorldState state;

        public static int totalClients = 0;
        public static int currentlyOnline = 0;
        public const int maxQueue = 4;
        private static readonly object clientsLock = new object();
        private static readonly object stateLock = new object();

        private string RSApublic;
        private string RSAprivate;

        private bool inGame;
        private bool startingGame;
        private HashSet<int> IDsInGameOrQueue; 

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
            IDsInGameOrQueue = new HashSet<int>();
            inGame = false;
            startingGame = false;
            state = new WorldState();
            state.addWorldInteractable(0, new Table(new PointF(1, 5), var: Table.Variation.lobby));

            for (int i = 1; i < 8; i++)
            {
                Workstation a = new Workstation(new PointF(board.cols - i, board.rows - 1));
                if (i % 3 == 0)
                    a.type = Workstation.stationType.pasta;
                //a.onInteract += printInteract;
                state.addWorldInteractable(i, a);

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

                    await Task.Run(() => handleClientTcpAsync(client));
                    
                }
                catch (Exception e)
                {
                    if (serverRunning)
                        Console.Write("Error accepting client: " + e.Message);
                }
            }
        }

        private async Task connectClientToGame(TcpClient client)
        {
            Invoke((MethodInvoker)delegate {
                label1.Text = "connecting client to game";
            });
            
            try
            {
                NetworkStream stream = client.GetStream();
                int clientId = Interlocked.Increment(ref totalClients);

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
                        byte[] playerData = UDP.createByteMessage(Data.Position, id, p.position.X, p.position.Y);
                        stateSyncList.AddRange(playerData);
                    }

                    allClients.Add(client);
                    IDUsingClients.Add(client, clientId);
                    playersUsingID.Add(clientId, new Player());
                    //copy playersusingid to currplayers
                }
                lock (stateLock)
                {
                    Console.WriteLine("Adding world data...");
                    stateSyncList.Add((byte)state.getInteractableCount());
                    stateSyncList.AddRange(state.getWorldState());
                    Console.WriteLine("Finished Adding");
                }
                byte byteId = (byte)clientId;

                //send new player joined to existing
                byte[] newPlayerBytes = new byte[1 + 1 + 4 + 4];
                newPlayerBytes[0] = (byte)Data.NewPlayer;
                Buffer.BlockCopy(UDP.createByteMessage(clientId, 6f, 6f), 0, newPlayerBytes, 1, 9);

                
                foreach (TcpClient existingClient in clientSnapshot.Keys)
                {
                    Console.WriteLine("ahhhhh");
                    await StreamHelp.WriteEncrypted(existingClient.GetStream(), newPlayerBytes, AESkeysUsingClients[client]);
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
                if (data.Length != 10)
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
                Dictionary<int, IPEndPoint> snapshot;
                lock (clientsLock)
                {
                    udpEndpointsUsingID[clientId] = res.RemoteEndPoint;
                    if (playersUsingID.TryGetValue(clientId, out Player player))
                    {
                        player.position = point;
                    }
                    else
                    {
                        Console.WriteLine($"Player with ID {clientId} not found. ");
                    }
                    snapshot = new Dictionary<int, IPEndPoint>(udpEndpointsUsingID);
                }

                foreach (var pair in snapshot) //todo fix??
                {
                    int otherId = pair.Key;
                    IPEndPoint ipEndPoint = new IPEndPoint(pair.Value.Address, UDP.regularCommunicationToClients);
                    if (otherId == clientId) continue;

                    try
                    {
                        //Console.WriteLine("Sending?");
                        await receiver.SendAsync(data, data.Length, ipEndPoint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to send to client {otherId}: {e.Message}");
                    }
                }

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
                                                        await Task.Run(() => connectClientToGame(client));
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
                            switch ((Data)databytebytes[0])
                            {
                                case Data.ObjInteract:
                                    //buffer = await StreamHelp.ReadEncrypted(stream, AESkeysUsingClients[client]); //FIX HERE
                                    if (databytebytes[1] == (byte)InteractionTypes.pickupPlate)
                                    {
                                        int clientId = (int)databytebytes[2];
                                        int interactableId = (int)databytebytes[3];
                                        byte[] successinteraction = new byte[2];
                                        bool flag = false;
                                        lock (stateLock)
                                        {
                                            if (state.interactWith(interactableId))
                                            {
                                                
                                                successinteraction[0] = (byte)Data.ObjInteractSuccess;
                                                successinteraction[1] = (byte)interactableId;
                                                flag = true;
                                            }
                                        }
                                        if (flag)
                                        {
                                            await StreamHelp.WriteEncryptedToAll(IDUsingClients.Keys.ToArray(), successinteraction, AESkeysUsingClients);
                                        }
                                    } 
                                    else if (databytebytes[1] == (byte)InteractionTypes.putdownPlate)
                                    {
                                        int clientId = (int)databytebytes[2];
                                        int interactableId = (int)databytebytes[3];
                                        byte[] successinteraction = new byte[2];
                                        bool flag = false;
                                        lock (stateLock)
                                        {
                                            if (state.interactWith(interactableId))
                                            {

                                                successinteraction[0] = (byte)Data.ObjInteractSuccess;
                                                successinteraction[1] = (byte)interactableId;
                                                flag = true;
                                            }
                                        }
                                        if (flag)
                                        {
                                            await StreamHelp.WriteEncryptedToAll(IDUsingClients.Keys.ToArray(), successinteraction, AESkeysUsingClients);
                                        }
                                    }
                                    break;
                                case Data.EnterQueue:
                                    Console.WriteLine("Received EnterQueue");
                                    if (!inGame)
                                    {
                                        Console.WriteLine("Not in game");
                                        bool flag = false;
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
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { } //TODO ADD
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
            startingGame = false;
            inGame = true;
            byte[] message = new byte[1];
            message[0] = (byte)Data.GameStart;
            await StreamHelp.WriteEncryptedToAll(IDUsingClients.Keys.ToArray(), message, AESkeysUsingClients);
        }
    }
}
