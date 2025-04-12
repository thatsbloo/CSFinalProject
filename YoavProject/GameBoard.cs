using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoavProject
{
    public partial class GameBoard : UserControl
    {
        private Image backgroundSpriteSheet;
        private int rows;
        private int cols;

        private int tileSize;

        public GameBoard()
        {
            InitializeComponent();

            SetStyle(ControlStyles.Selectable, false);
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;

            backgroundSpriteSheet = Image.FromFile("C:\\Users\\yoavt\\Documents\\YehudaProjects\\YoavProject\\YoavProject\\Assets\\Background.png");

            rows = 8;
            cols = 11; //needs to be odd number, or like, recommended. (starts at 0)
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            this.tileSize = Math.Min(this.Width / cols, this.Height / rows);
            base.OnPaint(e);

            // Set graphics settings to prevent blurriness
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            //start with the door
            //Console.WriteLine(this.Width + " " + (cols / 2-1)*temp + " " + (cols / 2+2)*temp + " " + temp);


            Pen p = new Pen(Color.Blue, 1);
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (j > 1)
                        e.Graphics.DrawImage(backgroundSpriteSheet, new Rectangle(tileSize * i, tileSize * j, tileSize, tileSize), new Rectangle(0, 0, 32, 32), GraphicsUnit.Pixel);
                    else
                        e.Graphics.DrawImage(backgroundSpriteSheet, new Rectangle(tileSize * i, tileSize * j, tileSize, tileSize), new Rectangle(0, 32, 32, 32), GraphicsUnit.Pixel);
                    //e.Graphics.DrawRectangle(p, new Rectangle(tileSize * i, tileSize*j, tileSize, tileSize));
                }
                //if (i % 2 == 0)
                //{
                //    e.Graphics.DrawRectangle(p, new Rectangle(tileSize * i, tileSize * j, tileSize, tileSize));
                //}
            }
            //e.Graphics.DrawRectangle(p, new Rectangle(tileSize * 4, tileSize * 0, tileSize * 6, tileSize));
            e.Graphics.DrawImage(backgroundSpriteSheet, new Rectangle(4 * tileSize, 0, 3 * tileSize, 2 * tileSize), new Rectangle(0, 96, 96, 64), GraphicsUnit.Pixel);
        }

        public int getTileSize()
        {
            return tileSize;
        }

        private void GameBoard_Load(object sender, EventArgs e)
        {
            
        }

        public void setDimensions(int width, int height)
        {
            Width = width;
            Height = height;
            this.Invalidate();
        }
    }
}
