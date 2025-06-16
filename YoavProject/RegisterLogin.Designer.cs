namespace YoavProject
{
    partial class RegisterLogin
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.username = new System.Windows.Forms.TextBox();
            this.password = new System.Windows.Forms.TextBox();
            this.info = new System.Windows.Forms.Label();
            this.login = new System.Windows.Forms.Button();
            this.register = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // username
            // 
            this.username.BackColor = System.Drawing.Color.Tan;
            this.username.ForeColor = System.Drawing.SystemColors.WindowText;
            this.username.Location = new System.Drawing.Point(199, 159);
            this.username.Name = "username";
            this.username.Size = new System.Drawing.Size(100, 26);
            this.username.TabIndex = 0;
            this.username.Text = "username";
            // 
            // password
            // 
            this.password.BackColor = System.Drawing.Color.Tan;
            this.password.Location = new System.Drawing.Point(163, 252);
            this.password.Name = "password";
            this.password.Size = new System.Drawing.Size(170, 26);
            this.password.TabIndex = 1;
            this.password.Text = "password";
            // 
            // info
            // 
            this.info.AutoSize = true;
            this.info.BackColor = System.Drawing.Color.Transparent;
            this.info.Location = new System.Drawing.Point(229, 363);
            this.info.Name = "info";
            this.info.Size = new System.Drawing.Size(0, 30);
            this.info.TabIndex = 2;
            // 
            // login
            // 
            this.login.BackColor = System.Drawing.Color.Tan;
            this.login.Location = new System.Drawing.Point(268, 457);
            this.login.Name = "login";
            this.login.Size = new System.Drawing.Size(117, 52);
            this.login.TabIndex = 3;
            this.login.Text = "Login";
            this.login.UseVisualStyleBackColor = false;
            this.login.Click += new System.EventHandler(this.login_Click);
            // 
            // register
            // 
            this.register.BackColor = System.Drawing.Color.Tan;
            this.register.Location = new System.Drawing.Point(32, 448);
            this.register.Name = "register";
            this.register.Size = new System.Drawing.Size(106, 61);
            this.register.TabIndex = 4;
            this.register.Text = "Register";
            this.register.UseVisualStyleBackColor = false;
            this.register.Click += new System.EventHandler(this.register_Click);
            // 
            // RegisterLogin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.register);
            this.Controls.Add(this.login);
            this.Controls.Add(this.info);
            this.Controls.Add(this.password);
            this.Controls.Add(this.username);
            this.Name = "RegisterLogin";
            this.Size = new System.Drawing.Size(527, 629);
            this.Load += new System.EventHandler(this.RegisterLogin_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox username;
        private System.Windows.Forms.TextBox password;
        private System.Windows.Forms.Label info;
        private System.Windows.Forms.Button login;
        private System.Windows.Forms.Button register;
        private System.Windows.Forms.Timer timer1;
    }
}
