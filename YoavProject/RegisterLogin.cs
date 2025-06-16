using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace YoavProject
{
    public partial class RegisterLogin : UserControl
    {
        public event Action loginPressed;
        public event Action registerPressed;
        private int tileSize;
        private int cols = 11;
        private int rows = 9;
        public RegisterLogin()
        {
            InitializeComponent();
        }

        public void setDimensions(int width, int height)
        {
            Width = width;
            Height = height;
            tileSize = Math.Min(this.Width / cols, this.Height / rows);
            username.Width = (int)(this.Width * 0.8);
            username.Height = tileSize / 2;
            username.Left = (int)(this.Width * 0.1);
            username.Top = tileSize * 3;
            password.Width = (int)(this.Width * 0.8);
            password.Top = tileSize * 4;
            password.Left = (int)(this.Width * 0.1);
            password.Height = tileSize / 2;
            info.Width = (int)(this.Width * 0.8);
            info.Top = tileSize * 5;
            info.Left = (int)(this.Width * 0.1);
            register.Left = tileSize;
            register.Top = tileSize * 6;
            login.Top = tileSize * 6;
            login.Left = this.ClientSize.Width - tileSize*2;
            login.Size = new Size(tileSize, (int)(tileSize * 0.8));
            register.Size = new Size(tileSize, (int)(tileSize * 0.8));

            this.Invalidate();
        }

        private void RegisterLogin_Load(object sender, EventArgs e)
        {
            
        }

        private void register_Click(object sender, EventArgs e)
        {
            registerPressed?.Invoke();
        }

        private void login_Click(object sender, EventArgs e)
        {
            loginPressed?.Invoke();
        }

        public bool areFieldsValid()
        {
            if (!isFieldValid(getUsername())) return false;
            if (!isFieldValid(getPassword())) return false;
            // check all characters are letters or digits
            return true;
        }

        public static bool isFieldValid(string field)
        {
            if (string.IsNullOrEmpty(field)) return false;
            if (!field.All(c => char.IsLetterOrDigit(c))) return false;
            return true;
        }

        public void displayErrorMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(displayErrorMessage), message);
                return;
            }

            info.Text = message;
            info.ForeColor = Color.Red;
        }

        public void displaySuccessMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(displaySuccessMessage), message);
                return;
            }

            info.Text = message;
            info.ForeColor = Color.Lime;
        }

        public string getUsername()
        {
            return username.Text;
        }

        public string getPassword()
        {
            return password.Text;
        }

        private Table aesthetics = new Table(size: new SizeF(9, 6), position: new PointF(1, 8), plates: 0);
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            drawBackground(e);

            float width = tileSize * 3;
            e.Graphics.DrawImage(Properties.Resources.Logo, new RectangleF(4*tileSize, 0, width, tileSize*2), new Rectangle(0, 0, 600, 400), GraphicsUnit.Pixel);

            aesthetics.draw(e.Graphics);

        }

        private void drawBackground(PaintEventArgs e)
        {
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    e.Graphics.DrawImage(GameBoard.backgroundSpriteSheet, new Rectangle(tileSize * i, tileSize * j, tileSize, tileSize), new Rectangle(0, 0, 32, 32), GraphicsUnit.Pixel);
                    //e.Graphics.DrawRectangle(p, new Rectangle(tileSize * i, tileSize*j, tileSize, tileSize));
                }
                //if (i % 2 == 0)
                //{
                //    e.Graphics.DrawRectangle(p, new Rectangle(tileSize * i, tileSize * j, tileSize, tileSize));
                //}
            }
            //e.Graphics.DrawRectangle(p, new Rectangle(tileSize * 4, tileSize * 0, tileSize * 6, tileSize));
            //e.Graphics.DrawImage(GameBoard.backgroundSpriteSheet, new Rectangle(4 * tileSize, 0, 3 * tileSize, 2 * tileSize), new Rectangle(0, 96, 96, 64), GraphicsUnit.Pixel);
        }
    }
}
