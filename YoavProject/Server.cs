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
            listener = new TcpListener(IPAddress.Any, UDP.regularCommunication);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();

            UDP.denyOthers();
            await acceptClientsAsync();
        }

        private async Task acceptClientsAsync()
        {
            while (serverRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    int clientId = Interlocked.Increment(ref totalClients);

                    lock (clientsLock)
                    {
                        allClients.Add(client);
                        activeClientsUsingID.Add(clientId, client);
                    }

                    Console.WriteLine($"Client connected with ID {clientId}");

                }
                catch (Exception e)
                {
                    if (serverRunning)
                        Console.Write("Error accepting client: " + e.Message);
                }
            }
        }

        private async Task handleClientUdpAsync(IPEndPoint endpoint, int id)
        {
            while (serverRunning)
            {
                UdpClient receiver = new UdpClient();
                receiver.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                receiver.Client.Bind(endpoint);
            }
        }

        private async void GameLoop_Tick(object sender, EventArgs e)
        {

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
