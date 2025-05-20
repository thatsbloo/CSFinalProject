namespace YoavProject
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServerButton = new System.Windows.Forms.Button();
            this.client = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ServerButton
            // 
            this.ServerButton.Location = new System.Drawing.Point(146, 94);
            this.ServerButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.ServerButton.Name = "ServerButton";
            this.ServerButton.Size = new System.Drawing.Size(246, 161);
            this.ServerButton.TabIndex = 0;
            this.ServerButton.Text = "Server";
            this.ServerButton.UseVisualStyleBackColor = true;
            this.ServerButton.Click += new System.EventHandler(this.ServerButton_Click);
            // 
            // client
            // 
            this.client.Location = new System.Drawing.Point(481, 149);
            this.client.Name = "client";
            this.client.Size = new System.Drawing.Size(213, 124);
            this.client.TabIndex = 1;
            this.client.Text = "Ckuebt";
            this.client.UseVisualStyleBackColor = true;
            this.client.Click += new System.EventHandler(this.client_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.client);
            this.Controls.Add(this.ServerButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ServerButton;
        private System.Windows.Forms.Button client;
    }
}

