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
