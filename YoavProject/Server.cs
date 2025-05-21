using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
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
        public static Dictionary<int, TcpClient> activeClientsUsingID = new Dictionary<int, TcpClient>();
        public static Dictionary<int, IPEndPoint> udpEndpointsUsingID = new Dictionary<int, IPEndPoint>();

        public static Dictionary<int, Player> playersUsingID = new Dictionary<int, Player>();

        public static int totalClients = 0;
        private static readonly object clientsLock = new object();
        public Server()
        {
            InitializeComponent();
        }

        private async void Server_Load(object sender, EventArgs e)
        {
            serverRunning = true;
            listener = new TcpListener(IPAddress.Any, UDP.regularCommunicationToServer);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();

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

                    int clientId = Interlocked.Increment(ref totalClients);

                    Dictionary<int, Player> currPlayers = new Dictionary<int, Player>();
                    List<byte> stateSyncList = new List<byte>();
                    lock (clientsLock)
                    {
                        stateSyncList.Add((byte)Data.StateSync);
                        stateSyncList.Add((byte)playersUsingID.Count);

                        foreach (var pair in playersUsingID)
                        {
                            int id = pair.Key;
                            Player p = pair.Value;

                            // Full message includes the PositionUpdate header
                            byte[] playerData = UDP.createByteMessage(Data.Position, id, p.position.X, p.position.Y);
                            stateSyncList.AddRange(playerData);
                        }

                        allClients.Add(client);
                        activeClientsUsingID.Add(clientId, client);
                        playersUsingID.Add(clientId, new Player());
                        //copy playersusingid to currplayers
                    }

                    Console.WriteLine($"Client connected with ID {clientId}");
                    NetworkStream stream = client.GetStream();
                    byte byteId = (byte)clientId;
                    await stream.WriteAsync(new byte[] { byteId }, 0, 1);
                    await stream.WriteAsync(stateSyncList.ToArray(), 0, stateSyncList.Count);



                }
                catch (Exception e)
                {
                    if (serverRunning)
                        Console.Write("Error accepting client: " + e.Message);
                }
            }
        }

        private async Task handleClientUdpAsync()
        {
            Console.WriteLine("hello??");
            UdpClient receiver = new UdpClient();
            receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, UDP.regularCommunicationToServer);
            receiver.Client.Bind(endpoint);

            async void process_data(UdpReceiveResult res)
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

                Console.WriteLine($"Received Type {messageType} | x: {x}, y: {y} from id: {clientId}");

                lock (clientsLock)
                {
                    udpEndpointsUsingID[clientId] = res.RemoteEndPoint;
                    if (playersUsingID.TryGetValue(clientId, out Player player))
                    {
                        player.position = point;
                    }
                    else
                    {
                        // Optional: handle case where player isn't found
                        Console.WriteLine($"Player with ID {clientId} not found.");
                    }
                }

                foreach (var pair in udpEndpointsUsingID)
                {
                    int otherId = pair.Key;
                    IPEndPoint ipEndPoint = new IPEndPoint(pair.Value.Address, UDP.regularCommunicationToClients);
                    if (otherId == clientId) continue;

                    try
                    {
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
                    process_data(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("UDP Receive error: " + ex.Message);
                }

            }

            receiver.Close();
        }

        private async void GameLoop_Tick(object sender, EventArgs e)
        {
            UdpClient broadcaster = new UdpClient();
            broadcaster.EnableBroadcast = true;

            IPEndPoint allEndPoints = new IPEndPoint(IPAddress.Broadcast, UDP.regularCommunicationToClients);
            //byte[] byteServerExists;

            //broadcaster.Send(byteServerExists, byteServerExists.Length, allEndPoints);
            //broadcaster.Client.ReceiveTimeout = 500;
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
