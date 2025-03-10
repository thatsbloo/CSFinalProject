using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoavProject
{
    public partial class Game : Form
    {

        private List<Player> online_players;
        private Player self;
        private GameBoard board;
        public Game()
        {
            InitializeComponent();

            self = new Player();
            online_players = new List<Player>();
            board = new GameBoard();
        }

        private void Game_Load(object sender, EventArgs e)
        {
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            Controls.Add(board);


        }

        private void Game_Resize(object sender, EventArgs e)
        {
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {

        }
    }
}
