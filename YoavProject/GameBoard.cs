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
        public GameBoard()
        {
            InitializeComponent();
            this.BackColor = Color.Black;
        }

        private void GameBoard_Load(object sender, EventArgs e)
        {
            
        }

        public void setDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
