using System;
using System.Windows.Forms;

namespace YoavProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ServerButton_Click(object sender, EventArgs e)
        {
            if (UDP.serverDoesntExist())
            {
                DialogResult = DialogResult.Yes;
            } else
            {
                MessageBox.Show("Server exists already >:(");
            }
        }

        private void client_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
        }
    }
}
