using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

        private Image backgroundSpriteSheet;

        private HashSet<Keys> pressedKeys;

        public static bool isDebugMode { get; private set; }

        public static bool connected { get; private set; }

        public Game()
        {
            InitializeComponent();


            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            
            this.DoubleBuffered = true;

            isDebugMode = false;

            //self = new Player();
            online_players = new List<Player>();
            board = new GameBoard();

            pressedKeys = new HashSet<Keys>();

        }

        private TcpClient tcpClient;
        public static int clientId { get; private set; }

        private async void Game_Load(object sender, EventArgs e)
        {
            Controls.Add(board);
            this.ClientSize = new Size(64 * board.cols, 64 * board.rows);
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            //Controls.Add(self);

            UDP.serverDoesntExist();
            tcpClient = new TcpClient(UDP.serverAddress.ToString(), UDP.regularCommunicationToServer);
            NetworkStream stream = tcpClient.GetStream();

            // read byte for client id
            byte[] buffer = new byte[1];
            await stream.ReadAsync(buffer, 0, 1);
            clientId = (int)buffer[0];
            connected = true;

            byte[] headerBuffer = new byte[2];
            await stream.ReadAsync(headerBuffer, 0, 2);

            byte packetType = headerBuffer[0];
            int playerCount = headerBuffer[1];

            if (packetType == (byte)Data.StateSync) // state sync
            {
                byte[] playersBuffer = new byte[playerCount * 10]; // each player: 1 byte id + + pos 4 float X + 4 float Y = 10 bytes
                await stream.ReadAsync(playersBuffer, 0, playersBuffer.Length);

                for (int i = 0; i < playerCount; i++)
                {
                    int offset = i * 10;
                    byte id = (byte)playersBuffer[offset];

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(playersBuffer, offset+2, 4);
                        Array.Reverse(playersBuffer, offset+6, 4);
                    }

                    float x = BitConverter.ToSingle(playersBuffer, offset + 2);
                    float y = BitConverter.ToSingle(playersBuffer, offset + 6);

                    Player p = new Player();
                    PointF pos = new PointF(x, y);
                    p.position = pos;

                    GameBoard.onlinePlayers.Add(id, p);
                }
            }

            _ = Task.Run(listenForUdpUpdatesAsync);


        }

        private async Task listenForUdpUpdatesAsync()
        {
            UdpClient listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            IPEndPoint serverEP = new IPEndPoint(UDP.serverAddress, UDP.regularCommunicationToClients);
            listener.Client.Bind(serverEP);
            while (connected)
            {
                try
                {
                    UdpReceiveResult res = await listener.ReceiveAsync();
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
            if (data.Length != 10)
            {
                Console.WriteLine("Invalid packet size");
                return;
            }

            byte messageType = data[0];
            byte senderId = data[1];

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data, 2, 4);
                Array.Reverse(data, 6, 4);
            }

            float x = BitConverter.ToSingle(data, 2);
            float y = BitConverter.ToSingle(data, 6);
            PointF position = new PointF(x, y);

            if (GameBoard.onlinePlayers.TryGetValue(senderId, out Player player))
            {
                player.position = position;
            }
            else
            {
                Console.WriteLine($"Received update for unknown player {senderId}");
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
            tcpClient.Close();
        }
    }
}
