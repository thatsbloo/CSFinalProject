using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                g.DrawRectangle(Pens.Gold, screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height);
            }
        }

    }
    public class Player : GameObject
    {

        public Player()
        {
            this.size = new SizeF(0.6f, 0.9f);
            this.hitboxSize = new SizeF(0.6f, 0.225f);

        }
        public override void draw(Graphics g)
        {
            
            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcSizeOnScreen();

            Console.WriteLine(screenPos.X + " " + (screenPos.Y-screenSize.Height) + " " + screenSize.Width + " " + screenSize.Height);


            g.FillRectangle(Brushes.Blue, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height));

            //g.FillRectangle(Brushes.Pink, new RectangleF(screenPos.X, screenPos.Y - 2, 2, 2));

            // g.FillEllipse(Brushes.Green, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, 5, 5));
            base.draw(g);
        }
    }

    

    public abstract class InteractableObject : GameObject
    {

    }

    class Costumer : InteractableObject 
    {
        public enum state { WalkingToTable, Thinking, Ordering, Waiting, Eating, WalkingAway }

        
        public override void draw(Graphics g)
        {
            this.size = new SizeF(0.6f, 0.9f);
            this.hitboxSize = new SizeF(0, 0);
        }


    }

    public class Table : InteractableObject
    {
        public int chairs;
        public int freeChairs;
        public Table() : this(new PointF(0f, 0f)) { }

        public Table(PointF position, int chairs = 4)
        {
            this.size = new SizeF(3, 2);
            this.hitboxSize = new SizeF(3, 2);
            this.position = position;
            this.chairs = chairs;
            this.freeChairs = chairs;
        }
        public override void draw(Graphics g)
        {
            
            var screenPos = calcPositionOnScreen(this.position);
            var screenSize = calcSizeOnScreen();

            //Console.WriteLine(screenPos.X + " " + (screenPos.Y-screenSize.Height) + " " + screenSize.Width + " " + screenSize.Height);


            g.DrawImage(GameBoard.backgroundSpriteSheet, new RectangleF(screenPos.X, screenPos.Y - screenSize.Height, screenSize.Width, screenSize.Height), new Rectangle(0, 192, 96, 64), GraphicsUnit.Pixel);
            g.DrawString(0 + "/" + chairs + " taken", new Font("Arial", 16), Brushes.Black, new PointF(screenPos.X, screenPos.Y - screenSize.Height));

            base.draw(g);
        }
    }

    class Workstation : InteractableObject
    {
        public enum stationType { empty, pasta, pizza, burger, coffee, sushiMAYBE }

        public stationType type { get; set; }
        public Workstation() : this(new PointF(0f, 0f)) { }

        public Workstation(PointF position)
        {
            this.size = new SizeF(1, 1.25f);
            this.hitboxSize = size;
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
    }
}

