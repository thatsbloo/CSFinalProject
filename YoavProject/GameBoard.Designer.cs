namespace YoavProject
{
    partial class GameBoard
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
            this.SuspendLayout();
            // 
            // GameBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "GameBoard";
            this.Size = new System.Drawing.Size(225, 231);
            this.Load += new System.EventHandler(this.GameBoard_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GameBoard_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.GameBoard_KeyUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
