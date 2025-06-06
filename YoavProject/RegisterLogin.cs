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
    public partial class RegisterLogin : UserControl
    {
        public event Action loginPressed;
        public event Action registerPressed;
        public RegisterLogin()
        {
            InitializeComponent();
        }

        private void RegisterLogin_Load(object sender, EventArgs e)
        {
            username.Width = (int)(this.Width * 0.8);
            username.Left = (int)(this.Width * 0.1);
            password.Width = (int)(this.Width * 0.8);
            password.Left = (int)(this.Width * 0.1);
            info.Width = (int)(this.Width * 0.8);
        }

        private void register_Click(object sender, EventArgs e)
        {

        }

        private void login_Click(object sender, EventArgs e)
        {

        }

        public bool areFieldsValid()
        {
            if (string.IsNullOrEmpty(password.Text)) return false;
            if (string.IsNullOrEmpty(username.Text)) return false;
            if (!password.Text.All(c => char.IsLetterOrDigit(c))) return false;
            if (!username.Text.All(c => char.IsLetterOrDigit(c))) return false;

            // Check all characters are letters or digits
            return true;
        }

        public void displayErrorMessage(string message)
        {
            info.Text = message;
            info.ForeColor = Color.Red;
        }

        public void displaySuccessMessage(string message)
        {
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
    }
}
