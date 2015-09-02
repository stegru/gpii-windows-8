using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Windows.Forms.Integration;
using System.Reflection;


namespace GPII
{
    public class GPIIApplicationContext : ApplicationContext
    {
        private System.ComponentModel.IContainer components;	// a list of components to dispose when the context is disposed
        private NotifyIcon notifyIcon;				            // the icon that sits in the system tray
        GPIIProximityListener p;
        GPIINodeManager nodeManager;

        public void LoginNotification() 
        {
            notifyIcon.BalloonTipTitle = "Logged in to GPII";
            notifyIcon.BalloonTipText = "Applying settings for the user on your tag.";
            notifyIcon.ShowBalloonTip(500);
        }

        public void LogoutNotification()
        {
            notifyIcon.BalloonTipTitle = "Logged out of GPII";
            notifyIcon.BalloonTipText = "Returning station to default settings.";
            notifyIcon.ShowBalloonTip(500);
        }

        /**
         * Currently to start this from the command line you're going to give the directory, executable,
         * and then any command line args necessary. So,
         * 
         * GPIIWindows8.exe "C:\Program Files (x86)\GPII\lgs-station" node.exe lgs_driver.js"
         */
        public GPIIApplicationContext(string[] args)
        {
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components)
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = new Icon("gpii.ico"),
                Text = "GPII Windows 8",
                Visible = true
            };
            notifyIcon.ContextMenuStrip.Items.Add(ToolStripMenuItemWithHandler("&Exit", exitItem_Click));

            nodeManager = new GPIINodeManager();
            if (args.Length == 3)
            {
                nodeManager.lgsWorkingDirectory = args[0];
                nodeManager.lgsFileName = args[1];
                nodeManager.lgsArguments = args[2];
            }
            nodeManager.startLocalGPII();

            try 
            {
                p = new GPIIProximityListener();
                p.applicationContext = this;
                p.InitializeProximityDevice();
            }
            catch (System.TypeLoadException tle)
            {
                // TODO Log something here in the system log.
            }
        }

        /// <summary>
        /// When the application context is disposed, dispose things like the notify icon.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) { components.Dispose(); }
        }

        private void exitItem_Click(object sender, EventArgs e)
        {
            // TODO Track our node processes adon't kill other peoples.
            foreach (Process proc in Process.GetProcessesByName("node"))
            {
                proc.Kill();
            }
            ExitThread();
        }

        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            notifyIcon.ContextMenuStrip.Items.Add(ToolStripMenuItemWithHandler("&Exit", exitItem_Click));
        }

        private ToolStripMenuItem ToolStripMenuItemWithHandler(
            string displayText, int enabledCount, int disabledCount, EventHandler eventHandler)
        {
            var item = new ToolStripMenuItem(displayText);
            if (eventHandler != null) { item.Click += eventHandler; }

            item.Image = null; 
            item.ToolTipText = "";
            return item;
        }

        public ToolStripMenuItem ToolStripMenuItemWithHandler(string displayText, EventHandler eventHandler)
        {
            return ToolStripMenuItemWithHandler(displayText, 0, 0, eventHandler);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //MessageBox.Show("Starting up GPII Windows 8");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var applicationContext = new GPIIApplicationContext(args);
                Application.Run(applicationContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Program Terminated Unexpectedly",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    
}