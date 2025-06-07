using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
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

        public static bool connected { get; private set; }

        public static bool signup { get; private set; }

        private static readonly object playersLock = new object();

        private string RSApublicKey;

        private string AESkey;

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

        private TcpClient tcpClient;
        public static int clientId { get; private set; }

        
        private async void Game_Load(object sender, EventArgs e)
        {
            //Controls.Add(board);
            Controls.Add(login);
            this.ClientSize = new Size(64 * board.cols, 64 * board.rows);
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            //Controls.Add(self);

            
            UDP.serverDoesntExist();
            tcpClient = new TcpClient(UDP.serverAddress.ToString(), StreamHelp.tcpPort);
            NetworkStream stream = tcpClient.GetStream();

            byte[] rsakeylength = await StreamHelp.ReadExactlyAsync(stream, 4);
            
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(rsakeylength);

            int rsaactuallength = BitConverter.ToInt32(rsakeylength, 0);
            byte[] RSApublic = await StreamHelp.ReadExactlyAsync(stream, rsaactuallength);

            RSApublicKey = Convert.ToBase64String(RSApublic);

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

            if (packetType == (byte)Data.CompleteStateSync) // state sync
            {
                byte[] playersBuffer = new byte[playerCount * 10]; // each player: 1 byte id + + pos 4 float X + 4 float Y = 10 bytes
                await stream.ReadAsync(playersBuffer, 0, playersBuffer.Length);

                for (int i = 0; i < playerCount; i++)
                {
                    int offset = i * 10;
                    byte id = (byte)playersBuffer[offset];

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(playersBuffer, offset + 2, 4);
                        Array.Reverse(playersBuffer, offset + 6, 4);
                    }

                    float x = BitConverter.ToSingle(playersBuffer, offset + 2);
                    float y = BitConverter.ToSingle(playersBuffer, offset + 6);

                    Player p = new Player();
                    PointF pos = new PointF(x, y);
                    p.position = pos;

                    lock (playersLock)
                    {
                        board.onlinePlayers.Add(id, p);
                        online_players.Add(p);
                    }

                }
            }

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
                    int msgtype = stream.ReadByte();

                    if (msgtype == -1)
                    {
                        Console.WriteLine("Disconnected [TCP].");
                        break;
                    }

                    switch ((Registration)msgtype)
                    {
                        case Registration.ErrorTaken:
                            login.displayErrorMessage("Username already taken!");
                            break;
                        case Registration.ErrorWrong:
                            login.displayErrorMessage("Incorrect username or password!");
                            break;
                        case Registration.ErrorInvalid:
                            login.displayErrorMessage("Invalid username!");
                            break;
                        case Registration.ErrorLoggedIn:
                            login.displayErrorMessage("This user is already logged in!");
                            break;
                        case Registration.RegisterSuccess:
                            login.displaySuccessMessage("Registration successful!");
                            break;
                        case Registration.LoginSuccess:
                            
                            login.Dispose();
                            Controls.Add(board);
                            signup = false;
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

            data.Add((byte)Registration.Login);
            string username = login.getUsername();
            string pass = login.getPassword();
            byte[] encryptedUsername = Encryption.encryptAES(Encoding.UTF8.GetBytes(username), AESkey);
            byte[] encryptedPassword = Encryption.encryptAES(Encoding.UTF8.GetBytes(pass), AESkey);

            int totalLength = encryptedUsername.Length + encryptedPassword.Length;
            byte[] lengthBytes = BitConverter.GetBytes(totalLength);
            byte[] usernameLengthBytes = BitConverter.GetBytes(encryptedUsername.Length);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
                Array.Reverse(usernameLengthBytes);
            }

            data.AddRange(lengthBytes);
            data.AddRange(usernameLengthBytes);
            data.AddRange(encryptedUsername);
            data.AddRange(encryptedPassword);

            await stream.WriteAsync(data.ToArray(), 0, data.Count);
        }

        private async void handleRegister()
        {
            if (!login.areFieldsValid())
            {
                login.displayErrorMessage("Username and Password must only include English letters and numbers!");
            }
            Stream stream = tcpClient.GetStream();
            List<byte> data = new List<byte>();

            data.Add((byte)Registration.Register);
            string username = login.getUsername();
            string pass = login.getPassword();

            byte[] encryptedUsername = Encryption.encryptAES(Encoding.UTF8.GetBytes(username), AESkey);
            byte[] encryptedPassword = Encryption.encryptAES(Encoding.UTF8.GetBytes(pass), AESkey);

            int totalLength = encryptedUsername.Length + encryptedPassword.Length;
            byte[] lengthBytes = BitConverter.GetBytes(totalLength);
            byte[] usernameLengthBytes = BitConverter.GetBytes(encryptedUsername.Length);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
                Array.Reverse(usernameLengthBytes);
            }

            data.AddRange(lengthBytes);
            data.AddRange(usernameLengthBytes);
            data.AddRange(encryptedUsername);
            data.AddRange(encryptedPassword);

            await stream.WriteAsync(data.ToArray(), 0, data.Count);
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

                    if (board.onlinePlayers.TryGetValue(data[2 + offset], out Player player))
                    {
                        player.position = playerposition;
                    }
                    else
                    {
                        Console.WriteLine($"Received update for unknown player {data[2+offset]}");
                    }
                }
                
            }
        }

        private async Task listenForTcpUpdatesAsync()
        {
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while(connected)
                {
                    int datatype = stream.ReadByte();

                    if (datatype == -1)
                    {
                        Console.WriteLine("Disconnected [TCP].");
                        break;
                    }

                    switch((Data)datatype)
                    {
                        case Data.NewPlayer:
                            byte[] res = await StreamHelp.ReadExactlyAsync(stream, 9);

                            byte playerId = res[0];
                            float posX = BitConverter.ToSingle(res, 1);
                            float posY = BitConverter.ToSingle(res, 5);

                            Console.WriteLine($"New player joined! ID: {playerId}");

                            Player p = new Player();
                            p.position = new PointF(posX, posY);
                            
                            lock (playersLock)
                            {
                                board.onlinePlayers.Add(playerId, p);
                                online_players.Add(p);
                            }
                            break;
                        default:
                            break;

                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
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
