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
            this.ServerButton.Location = new System.Drawing.Point(223, 236);
            this.ServerButton.Name = "ServerButton";
            this.ServerButton.Size = new System.Drawing.Size(293, 176);
            this.ServerButton.TabIndex = 0;
            this.ServerButton.Text = "Server";
            this.ServerButton.UseVisualStyleBackColor = true;
            this.ServerButton.Click += new System.EventHandler(this.ServerButton_Click);
            // 
            // client
            // 
            this.client.Location = new System.Drawing.Point(689, 236);
            this.client.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.client.Name = "client";
            this.client.Size = new System.Drawing.Size(293, 176);
            this.client.TabIndex = 1;
            this.client.Text = "Client";
            this.client.UseVisualStyleBackColor = true;
            this.client.Click += new System.EventHandler(this.client_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 692);
            this.Controls.Add(this.client);
            this.Controls.Add(this.ServerButton);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ServerButton;
        private System.Windows.Forms.Button client;
    }
}

