using System;
using System.Collections.Generic;
using System.Drawing;

namespace YoavProject
{
    public abstract class GameObject
    {

        public PointF position { get; set; } //in tiles
        public SizeF size { get; set; } //in tiles
        public SizeF hitboxSize { get; set; } //in tiles


        public SizeF calcSizeOnScreen()
        {
            int tileSize = GameBoard.tileSize;
            return new SizeF(tileSize * size.Width, tileSize * size.Height);
        }

        public SizeF calcHitboxSizeOnScreen()
        {
            int tileSize = GameBoard.tileSize;
            return new SizeF(tileSize * hitboxSize.Width, tileSize * hitboxSize.Height);
        }

        public PointF calcPositionOnScreen(PointF position)
        {
            int tileSize = GameBoard.tileSize;
            return new PointF(tileSize * position.X, tileSize * position.Y);
        }


        public RectangleF getCollisionArea()
        {
            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcHitboxSizeOnScreen();

            return new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height);
        }



        public (float yUp, float yDown, float xLeft, float xRight) getBorders()
        {
            return (position.Y - size.Height, position.Y, position.X, position.X + size.Width);
        }

        public virtual void draw(Graphics g)
        {
            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcHitboxSizeOnScreen();

            if (Game.isDebugMode)
            {
                g.DrawRectangle(Pens.Red, screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height);
            }
        }

    }
    public class Player : GameObject
    {
        public int platesHeld;
        public Player()
        {
            this.size = new SizeF(0.6f, 0.9f);
            this.hitboxSize = new SizeF(0.6f, 0.225f);
            this.platesHeld = 0;

        }

        public void addPlate()
        {
            if (this.platesHeld < 3)
            {
                Console.WriteLine("aa");
                this.platesHeld++;
            }
        }

        public override void draw(Graphics g)
        {
            
            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcSizeOnScreen();

            //Console.WriteLine(screenPos.X + " " + (screenPos.Y-screenSize.Height) + " " + screenSize.Width + " " + screenSize.Height);


            g.FillRectangle(Brushes.Blue, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height));
            //for (int i = 0; i < platesHeld; i++) 
            //{
            //    g.FillRectangle(Brushes.Red, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height));
            //}
            g.DrawString(platesHeld + " plates", new Font("Arial", 16), Brushes.Black, new PointF(screenPos.X, screenPos.Y - screenSize.Height));
            //g.FillRectangle(Brushes.Pink, new RectangleF(screenPos.X, screenPos.Y - 2, 2, 2));

            // g.FillEllipse(Brushes.Green, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, 5, 5));
            base.draw(g);
        }
    }

    

    public abstract class InteractableObject : GameObject
    {
        public enum Types: byte { Table = 0, Workstation = 1 }
        public static float interactionRange = 0.5f;
        public bool highlighted = false;
        public event Func<bool> onInteract;

        public override void draw(Graphics g)
        {
            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcHitboxSizeOnScreen();

            if (highlighted)
            {
                g.DrawRectangle(Pens.Gold, screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height);
            }
            base.draw(g);
        }

        public bool interact()
        {
            if (onInteract != null)
            {
                foreach (Func<bool> handler in onInteract.GetInvocationList())
                {
                    if (handler.Invoke())
                        return true;
                }
            }
            return false;
        }

        public abstract byte[] getByteData();
    }

    //class Costumer : InteractableObject 
    //{
    //    public enum state { WalkingToTable, Thinking, Ordering, Waiting, Eating, WalkingAway }


    //    public override void draw(Graphics g)
    //    {
    //        this.size = new SizeF(0.6f, 0.9f);
    //        this.hitboxSize = new SizeF(0, 0);
    //    }


    //}

    public class Table : InteractableObject
    {
        public enum Variation: byte { lobby = 0, var1 = 1, var2 = 2, var3 = 3 }

        public int chairs;
        public int takenChairs;
        private int platesOnTable;
        Variation var;
        public Table() : this(new PointF(0f, 0f)) { }

        public Table(PointF position, SizeF? size = null, int chairs = 4, int takenChairs = 0, int plates = 3, Variation var = Variation.lobby)
        {
            this.size = size ?? new SizeF(3, 2);
            this.hitboxSize = this.size;
            this.position = position;
            this.chairs = chairs;
            this.takenChairs = takenChairs;
            this.platesOnTable = plates;
            this.var = var;
            this.onInteract += interactWithMe;
        }
        public override void draw(Graphics g)
        {

            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcSizeOnScreen();

            //Console.WriteLine(screenPos.X + " " + (screenPos.Y-screenSize.Height) + " " + screenSize.Width + " " + screenSize.Height);


            g.DrawImage(GameBoard.backgroundSpriteSheet, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height), new Rectangle(0, 192, 96, 64), GraphicsUnit.Pixel);
            if (this.var == Variation.lobby)
                g.DrawString(0 + "/" + chairs + " taken", new Font("Arial", 16), Brushes.Black, new PointF(screenPos.X, screenPos.Y - screenSize.Height));
            else 
                g.DrawString(platesOnTable + " plates", new Font("Arial", 16), Brushes.Black, new PointF(screenPos.X, screenPos.Y - screenSize.Height));

            base.draw(g);
        }

        private bool interactWithMe()
        {
            if (this.var == Variation.lobby)
            {
                if (this.chairs > this.takenChairs)
                {
                    this.takenChairs++;
                    return true;
                }
                return false;
            }
            else
            {
                if (this.platesOnTable > 0)
                {
                    this.platesOnTable--;
                    Console.WriteLine("New plate count: " + platesOnTable);
                    return true;
                }
                Console.WriteLine("New plate count: " + platesOnTable);
                return false;
            }
        }

        public override byte[] getByteData()
        {
            List<byte> data = new List<byte>();
            data.Add((byte)Types.Table);

            data.Add((byte)this.var);
            data.Add((byte)chairs);
            data.Add((byte)takenChairs);
            data.Add((byte)platesOnTable);
            byte[] posx = BitConverter.GetBytes(position.X);
            byte[] posy = BitConverter.GetBytes(position.Y);
            byte[] sizew = BitConverter.GetBytes(size.Width);
            byte[] sizeh = BitConverter.GetBytes(size.Height);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(posx);
                Array.Reverse(posy);
                Array.Reverse(sizew);
                Array.Reverse(sizeh);
            }
            data.AddRange(posx);
            data.AddRange(posy);
            data.AddRange(sizew);
            data.AddRange(sizeh);
            //byte[] res = new byte[1 + data.Count];
            //res[0] = (byte)data.Count;
            //Array.Copy(data.ToArray(), 0, res, 1, data.Count);
            Console.WriteLine(data.ToArray().ToString());
            return data.ToArray();
        }
    }

    class Workstation : InteractableObject
    {
        public enum stationType: byte { empty = 0, pasta = 1, pizza = 2, burger = 3, coffee = 4, sushiMAYBE = 5 }

        public stationType type { get; set; }
        public Workstation() : this(new PointF(0f, 0f)) { }

        public Workstation(PointF position, stationType type = stationType.empty)
        {
            this.size = new SizeF(1, 1.25f);
            this.hitboxSize = this.size;
            this.type = type;
            this.position = position;
        }
        public override void draw(Graphics g)
        {
            

            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcSizeOnScreen();

            //Console.WriteLine(screenPos.X + " " + (screenPos.Y-screenSize.Height) + " " + screenSize.Width + " " + screenSize.Height);

            g.DrawImage(GameBoard.backgroundSpriteSheet, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height), new Rectangle(128, 160, 32, 40), GraphicsUnit.Pixel);
            //g.FillRectangle(Brushes.Pink, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height));

            switch(type)
            {
                case stationType.pasta:
                    g.DrawImage(GameBoard.backgroundSpriteSheet, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height), new Rectangle(160, 160, 32, 40), GraphicsUnit.Pixel);
                    break;
                case stationType.empty:
                    break;
                default:
                    break;
                
            }

            base.draw(g);
        }

        public override byte[] getByteData()
        {
            List<byte> data = new List<byte>();
            data.Add((byte)Types.Workstation);

            data.Add((byte)this.type);
            byte[] posx = BitConverter.GetBytes(position.X);
            byte[] posy = BitConverter.GetBytes(position.Y);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(posx);
                Array.Reverse(posy);
            }
            data.AddRange(posx);
            data.AddRange(posy);
            //byte[] res = new byte[1 + data.Count];
            //res[0] = (byte)data.Count;
            //Array.Copy(data.ToArray(), 0, res, 1, data.Count);
            Console.WriteLine(data.ToArray().ToString());
            return data.ToArray();
        }
    }
}

