namespace YoavProject
{
    partial class Server
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
            this.components = new System.ComponentModel.Container();
            this.GameLoop = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.GameCountdown = new System.Windows.Forms.Timer(this.components);
            this.GameRound = new System.Windows.Forms.Timer(this.components);
            this.GameInterval = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // GameLoop
            // 
            this.GameLoop.Enabled = true;
            this.GameLoop.Interval = 33;
            this.GameLoop.Tick += new System.EventHandler(this.GameLoop_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(375, 149);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "label1";
            // 
            // GameCountdown
            // 
            this.GameCountdown.Interval = 5000;
            this.GameCountdown.Tick += new System.EventHandler(this.GameCountdown_Tick);
            // 
            // GameRound
            // 
            this.GameRound.Interval = 15000;
            this.GameRound.Tick += new System.EventHandler(this.GameRound_Tick);
            // 
            // GameInterval
            // 
            this.GameInterval.Interval = 3000;
            this.GameInterval.Tick += new System.EventHandler(this.GameInterval_Tick);
            // 
            // Server
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 449);
            this.Controls.Add(this.label1);
            this.Name = "Server";
            this.Text = "Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Server_FormClosing);
            this.Load += new System.EventHandler(this.Server_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer GameLoop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer GameCountdown;
        private System.Windows.Forms.Timer GameRound;
        private System.Windows.Forms.Timer GameInterval;
    }
}