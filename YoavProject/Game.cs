using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
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

        public Game()
        {
            InitializeComponent();


            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.ClientSize = new Size(64*11, 64*9);
            this.DoubleBuffered = true;

            isDebugMode = false;

            //self = new Player();
            online_players = new List<Player>();
            board = new GameBoard();

            pressedKeys = new HashSet<Keys>();

        }

        private TcpClient tcpClient;

        private void Game_Load(object sender, EventArgs e)
        {
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            Controls.Add(board);
            //Controls.Add(self);
            UDP.serverDoesntExist();
            tcpClient = new TcpClient(UDP.serverAddress.ToString(), UDP.regularCommunication);



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
