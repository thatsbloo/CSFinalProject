﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        public static Dictionary<int, TcpClient> activeClientsUsingID = new Dictionary<int, TcpClient>();
        public static Dictionary<int, IPEndPoint> udpEndpointsUsingID = new Dictionary<int, IPEndPoint>();

        public static Dictionary<TcpClient, string> AESkeysUsingClients = new Dictionary<TcpClient, string>();

        public static Dictionary<int, Player> playersUsingID = new Dictionary<int, Player>();

        public static int totalClients = 0;
        private static readonly object clientsLock = new object();

        private string RSApublic;
        private string RSAprivate;

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

            (RSAprivate, RSApublic) = Encryption.generateRSAkeypair();
            Console.WriteLine(RSApublic);
            JsonHandler = new JsonHandler();

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
            try
            {
                NetworkStream stream = client.GetStream();
                int clientId = Interlocked.Increment(ref totalClients);

                List<byte> stateSyncList = new List<byte>();
                Dictionary<int, TcpClient> clientSnapshot;
                lock (clientsLock)
                {
                    clientSnapshot = new Dictionary<int, TcpClient>(activeClientsUsingID);
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
                    activeClientsUsingID.Add(clientId, client);
                    playersUsingID.Add(clientId, new Player());
                    //copy playersusingid to currplayers
                }
                byte byteId = (byte)clientId;

                //send new player joined to existing
                byte[] newPlayerBytes = new byte[1 + 1 + 4 + 4];
                newPlayerBytes[0] = (byte)Data.NewPlayer;
                Buffer.BlockCopy(UDP.createByteMessage(clientId, 6f, 6f), 0, newPlayerBytes, 1, 9);


                foreach (TcpClient existingClient in clientSnapshot.Values)
                {
                    Console.WriteLine("ahhhhh");
                    await existingClient.GetStream().WriteAsync(newPlayerBytes, 0, newPlayerBytes.Length);
                }
                Console.WriteLine($"Client connected with ID {clientId}");


                //send id and then the positions of other clients to new client
                await stream.WriteAsync(new byte[] { byteId }, 0, 1);
                await stream.WriteAsync(stateSyncList.ToArray(), 0, stateSyncList.Count);
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
                        int databyte = stream.ReadByte();

                        if (databyte == -1)
                        {
                            Console.WriteLine("Disconnected Client[TCP].");
                            break;
                        }

                        if (!isSignedIn)
                        {
                            Console.WriteLine("reading length of everything");
                            byte[] lengthbyte = await StreamHelp.ReadExactlyAsync(stream, 4);
                            if (!BitConverter.IsLittleEndian)
                                Array.Reverse(lengthbyte);
                            Console.WriteLine("converting length to int32");
                            int length = BitConverter.ToInt32(lengthbyte, 0);
                            Console.WriteLine(length);

                            Console.WriteLine("reading length of username");
                            byte[] usernamelengthbyte = await StreamHelp.ReadExactlyAsync(stream, 4);
                            if (!BitConverter.IsLittleEndian)
                                Array.Reverse(usernamelengthbyte);
                            Console.WriteLine("converting length to int32");
                            int usernamelength = BitConverter.ToInt32(usernamelengthbyte, 0);
                            Console.WriteLine(usernamelength);

                            byte[] usernameinbytesenc = await StreamHelp.ReadExactlyAsync(stream, usernamelength);
                            byte[] passwordinbytesenc = await StreamHelp.ReadExactlyAsync(stream, length - usernamelength);
                            Console.WriteLine("read username and password");

                            string username = Encoding.UTF8.GetString(Encryption.decryptAES(usernameinbytesenc, AESkeysUsingClients[client]));
                            Console.WriteLine(username);
                            if (RegisterLogin.isFieldValid(username))
                            {
                                switch ((Registration)databyte)
                                {
                                    case Registration.Register:
                                        Console.WriteLine("register");
                                        if (JsonHandler.userExists(username))
                                            stream.WriteByte((byte)Registration.ErrorTaken);
                                        else
                                        {
                                            string password = Encoding.UTF8.GetString(Encryption.decryptAES(passwordinbytesenc, AESkeysUsingClients[client]));
                                            if (RegisterLogin.isFieldValid(password))
                                            {
                                                JsonHandler.addUser(username, password);
                                                stream.WriteByte((byte)Registration.RegisterSuccess);
                                            }
                                            else
                                            {
                                                stream.WriteByte((byte)Registration.ErrorInvalid);
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
                                                string password = Encoding.UTF8.GetString(Encryption.decryptAES(passwordinbytesenc, AESkeysUsingClients[client]));
                                                if (RegisterLogin.isFieldValid(password))
                                                {
                                                    if (JsonHandler.verifyLogin(username, password))
                                                    {
                                                        stream.WriteByte((byte)Registration.LoginSuccess);
                                                        isSignedIn = true;
                                                        await Task.Run(() => connectClientToGame(client));
                                                    }
                                                    else
                                                    {
                                                        stream.WriteByte((byte)Registration.ErrorWrong);
                                                    }
                                                }
                                                else
                                                {
                                                    stream.WriteByte((byte)Registration.ErrorInvalid);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            stream.WriteByte((byte)Registration.ErrorWrong);
                                        }

                                        break;

                                }
                            }
                            else
                            {
                                stream.WriteByte((byte)Registration.ErrorInvalid);
                            }
                        }
                        else
                        {

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
    }
}
