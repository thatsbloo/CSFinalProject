using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace YoavProject
{
    public partial class GameBoard : UserControl
    {
        public static Image backgroundSpriteSheet {  get; private set; }
        public int rows {  get; private set; }
        public int cols { get; private set; }
        private int wallHeight;
        public Player player { get; private set; }

        public static int tileSize { get; private set; }

        private HashSet<Keys> pressedKeys;

        private List<InteractableObject> interactables;
        private List<Costumer> costumers;

        public Dictionary<int, Player> onlinePlayers;

        private GameObject topWall;

        private float playerSpeed = 3.5f; //in tiles



        public GameBoard()
        {
            InitializeComponent();

            SetStyle(ControlStyles.Selectable, false);
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;

            backgroundSpriteSheet = Properties.Resources.Background;

            rows = 9;
            cols = 12; //needs to be odd number, or like, recommended because door. (starts at 0 (from 0 to 10))
            wallHeight = 2;

            player = new Player();
            player.position = new Point(6,6);
            Console.WriteLine(player.getBorders());

            pressedKeys = new HashSet<Keys>();

            interactables = new List<InteractableObject>();
            costumers = new List<Costumer>();

            onlinePlayers = new Dictionary<int, Player>();

            interactables.Add(new Table(new PointF(1, 5)));

            for (int i = 0; i < 7; i++)
            {
                Workstation a = new Workstation(new PointF(cols - i, rows - 1));
                if (i % 3 == 0)
                    a.type = Workstation.stationType.pasta;
                interactables.Add(a);
                
            }

            

            tileSize = Math.Min(this.Width / cols, this.Height / rows);
        }

        public void Update()
        {
            #region proccess keys
            (int, int) direction = (0, 0);
            if (pressedKeys.Contains(Keys.W))
            {
                direction.Item2 += -1;
            }
            if (pressedKeys.Contains(Keys.A))
            {
                direction.Item1 += -1;
            }
            if (pressedKeys.Contains(Keys.S))
            {
                direction.Item2 += 1;
            }
            if (pressedKeys.Contains(Keys.D))
            {
                direction.Item1 += 1;
            }
            movePlayer(direction);
            #endregion

            this.Invalidate();
        }

        private void movePlayer((int, int) direction)
        {

            

            float move = playerSpeed / 30; // 30 being the updates per second according to the gameloop timer

            float dirx = direction.Item1;
            float diry = direction.Item2;

            // normalize if moving diagonally
            float length = (float)Math.Sqrt(dirx * dirx + diry * diry);
            if (length > 0)
            {
                dirx /= length; 
                diry /= length;
            }


            PointF pos = player.position;
            pos.X += dirx * move;
            pos.Y += diry * move;

            #region collision handling
            void handle_borders(float yUp, float yDown, float xLeft, float xRight)
            {
                if (pos.Y < yUp) pos.Y = yUp;
                if (pos.Y > yDown) pos.Y = yDown;
                if (pos.X < xLeft) pos.X = xLeft;
                if (pos.X+player.size.Width > xRight) pos.X = xRight- player.size.Width;
            }

          

            void handle_objects(InteractableObject obj)
            {
                SizeF screenSize = player.calcHitboxSizeOnScreen();
                PointF screenPos = player.calcPositionOnScreen(pos);
                RectangleF playerRect = new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height);
                RectangleF objRect = obj.getCollisionArea();

                if (playerRect.Right >= objRect.Left && playerRect.Left <= objRect.Right && playerRect.Bottom >= objRect.Top && playerRect.Top <= objRect.Bottom)
                {
                    Console.WriteLine("amor de mis amores");

                    // Calculate overlap on each side
                    float overlapLeft = playerRect.Right - objRect.Left;
                    float overlapRight = objRect.Right - playerRect.Left;
                    float overlapTop = playerRect.Bottom - objRect.Top;
                    float overlapBottom = objRect.Bottom - playerRect.Top;

                    // Find the smallest overlap
                    float minHorizontal = Math.Min(overlapLeft, overlapRight);
                    float minVertical = Math.Min(overlapTop, overlapBottom);

                    // Adjust position based on which direction has the smaller overlap
                    if (minHorizontal < minVertical)
                    {
                        // Push horizontally
                        if (overlapLeft < overlapRight)
                            pos.X -= overlapLeft / tileSize;
                        else
                            pos.X += overlapRight / tileSize;
                    }
                    else
                    {
                        // Push vertically
                        if (overlapTop < overlapBottom)
                            pos.Y -= overlapTop / tileSize;
                        else
                            pos.Y += overlapBottom / tileSize;
                    }
                }
            }

            void handle_interactables()
            {
                foreach (InteractableObject obj in interactables)
                {
                    //Console.WriteLine(obj.getBorders());
                    if (obj == null) continue;
                    
                    //handle_borders(borders.Item1, borders.Item2, borders.Item3, borders.Item4);
                    handle_objects(obj);

                }

            }
            #endregion

            handle_borders(wallHeight, rows, 0, cols);
            handle_interactables();

            if (Game.connected)
            {
                byte[] messageToSend = UDP.createByteMessage(Data.Position, pos.X, pos.Y);
                UDP.sendToServer(messageToSend);
            }
            player.position = pos;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            
            base.OnPaint(e);

            // Set graphics settings to prevent blurriness
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            drawBackground(e);

            Random rnd = new Random();

            foreach (InteractableObject obj in interactables)
            {
                if (obj == null) continue;
                obj.draw(e.Graphics);
            }

            //player.position = new PointF((float)(rnd.NextDouble()*rows), (float)(rnd.NextDouble() * cols));
            //Console.WriteLine(player.position);
            if (Game.connected)
            {
                foreach (var pair  in onlinePlayers)
                {
                    pair.Value.draw(e.Graphics);
                }
            }
            player.draw(e.Graphics);

            
            //d
        }

        private void drawBackground(PaintEventArgs e)
        {
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
            tileSize = Math.Min(this.Width / cols, this.Height / rows);
            this.Invalidate();
        }

        private void GameBoard_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode);
        }

        private void GameBoard_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
        }
    }
}
