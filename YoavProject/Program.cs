﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoavProject
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Game());
            var reply = new Form1().ShowDialog();
            if (reply == DialogResult.Yes)
            {
                Application.Run(new Server());
            }
            if (reply == DialogResult.No)
            {
                Application.Run(new Game());
            }
        }
    }
}
