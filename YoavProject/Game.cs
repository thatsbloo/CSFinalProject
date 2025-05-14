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

        private void Game_Load(object sender, EventArgs e)
        {
            board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
            Controls.Add(board);
            //Controls.Add(self);


        }

        private void Game_Resize(object sender, EventArgs e)
        {
            //board.setDimensions(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            
            board.Focus();
            board.Update();
            //#region proccess keys
            //(int, int) direction = (0, 0);
            //if (pressedKeys.Contains(Keys.W))
            //{
            //    direction.Item2 += -1;
            //}
            //else if (pressedKeys.Contains(Keys.A))
            //{
            //    direction.Item1 += -1;
            //}
            //else if (pressedKeys.Contains(Keys.S))
            //{
            //    direction.Item2 += 1;
            //}
            //else if (pressedKeys.Contains(Keys.D))
            //{
            //    direction.Item1 += 1;
            //}
            //move_player(direction);
            //#endregion


           
            //Console.WriteLine(pressedKeys);
        }

        private void move_player((int, int) direction)
        {
            Console.WriteLine(direction);
            int move = board.getTileSize() / 8;
            var pos = board.player.position;
            pos.X += direction.Item1 * move;
            pos.Y += direction.Item2 * move;
            board.player.position = pos;

            //self.Top += direction.Item2 * move;
            //Console.WriteLine(self.Top + " " + self.Left);
        }

        //private void Game_KeyDown(object sender, KeyEventArgs e)
        //{
        //    //Console.WriteLine(e.KeyCode);
        //    pressedKeys.Add(e.KeyCode);
        //}

        //private void Game_KeyUp(object sender, KeyEventArgs e)
        //{
        //    pressedKeys.Remove(e.KeyCode);
        //}
    }
}
