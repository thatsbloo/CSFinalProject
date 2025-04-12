using System;
using System.Collections;
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

        private Image backgroundSpriteSheet;

        private HashSet<Keys> pressedKeys;

        public Game()
        {
            InitializeComponent();


            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.ClientSize = new Size(64*11, 64*8);

            self = new Player();
            online_players = new List<Player>();
            board = new GameBoard();

            pressedKeys = new HashSet<Keys>();

        }

        private void Game_Load(object sender, EventArgs e)
        {
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            //Controls.Add(board);
            Controls.Add(self);


        }

        private void Game_Resize(object sender, EventArgs e)
        {
            //board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            this.Focus();
            #region proccess keys
            (int, int) direction = (0, 0);
            if (pressedKeys.Contains(Keys.W))
            {
                direction.Item2 += -1;
            }
            else if (pressedKeys.Contains(Keys.A))
            {
                direction.Item1 += -1;
            }
            else if (pressedKeys.Contains(Keys.S))
            {
                direction.Item2 += 1;
            }
            else if (pressedKeys.Contains(Keys.D))
            {
                direction.Item1 += 1;
            }
            move_player(direction);
            #endregion


            
            //Console.WriteLine(pressedKeys);
        }

        private void move_player((int, int) direction)
        {
            Console.WriteLine(direction);
            int move = board.getTileSize() / 8;
            self.Left = direction.Item1 * move;
            self.Top = direction.Item2 * move;
        }

        private void Game_KeyDown(object sender, KeyEventArgs e)
        {
            //Console.WriteLine(e.KeyCode);
            pressedKeys.Add(e.KeyCode);
        }

        private void Game_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
        }
    }
}
