using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Windows.Networking.Proximity;
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

        public void LoginNotification() 
        {
            notifyIcon.BalloonTipTitle = "Logged in to GPII";
            notifyIcon.BalloonTipText = "Applying settings for the user on your tag.";
            notifyIcon.ShowBalloonTip(1000);
        }

        public void LogoutNotification()
        {
            notifyIcon.BalloonTipTitle = "Logged out of GPII";
            notifyIcon.BalloonTipText = "Returning station to default settings.";
            notifyIcon.ShowBalloonTip(1000);
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

            p = new GPIIProximityListener();
            if (args.Length == 3)
            {
                p.lgsWorkingDirectory = args[0];
                p.lgsFileName = args[1];
                p.lgsArguments = args[2];
            }
            p.startLocalGPII();
            p.applicationContext = this;
            p.InitializeProximityDevice();
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
            //p.localGPIIProcess.Kill();
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

    class GPIIProximityListener
    {
        ProximityDevice proximityDevice;
        bool loggedIn = false;
        string loggedInUser = "";
        DateTime lastTapTime = DateTime.UtcNow;

        public Process localGPIIProcess = null;

        public String lgsWorkingDirectory = "C:\\Program Files (x86)\\gpii\\windows";
        public String lgsFileName = "grunt";
        public String lgsArguments = "start";
        public GPIIApplicationContext applicationContext;

        public void startLocalGPII()
        {
            ProcessStartInfo gpiiStartInfo = new ProcessStartInfo();
            gpiiStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            gpiiStartInfo.WorkingDirectory = lgsWorkingDirectory;
            gpiiStartInfo.FileName = lgsFileName;
            gpiiStartInfo.Arguments = lgsArguments;
            localGPIIProcess = Process.Start(gpiiStartInfo);
        }

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

        public void InitializeProximityDevice()
        {
            proximityDevice = Windows.Networking.Proximity.ProximityDevice.GetDefault();

            if (proximityDevice != null)
            {
                proximityDevice.DeviceArrived += ProximityDeviceArrived;
                proximityDevice.DeviceDeparted += ProximityDeviceDeparted;
                proximityDevice.SubscribeForMessage("NDEF", messageReceivedHandler);
                WriteMessageText("Proximity device initialized.\n");
            }
            else
            {
                WriteMessageText("Failed to initialized proximity device.\n");
            }
        }

        public void gpiiLogin(string userToken)
        {
            var url = "http://localhost:8081/user/" + userToken + "/login";
            WriteMessageText(url);
            using (WebClient client = new WebClient())
            {
                string s = client.DownloadString(url);
            }

            applicationContext.LoginNotification();
        }

        public void gpiiLogout(string userToken)
        {
            var url = "http://localhost:8081/user/" + userToken + "/logout";
            WriteMessageText(url);
            using (WebClient client = new WebClient())
            {
                string s = client.DownloadString(url);
            }

            applicationContext.LogoutNotification();
        }

        public string readUserTokenFromTag(ProximityMessage message)
        {
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(message.Data);
            byte[] bytes = new byte[message.Data.Length];
            dataReader.ReadBytes(bytes);
            var data = Encoding.ASCII.GetString(bytes, 0, (int)message.Data.Length);
            var userToken = data.Substring(7);
            return userToken;
        }

        public void messageReceivedHandler(ProximityDevice device, ProximityMessage message)
        {
            DateTime thisTapTime = DateTime.UtcNow;
            TimeSpan sinceLastTapIn = thisTapTime - lastTapTime;
            WriteMessageText("Time since last tap (sec): " + sinceLastTapIn.TotalSeconds);
            if (sinceLastTapIn.TotalSeconds < 3)
            {
                return;
            }
            else
            {
                lastTapTime = thisTapTime;
            }
            var userToken = readUserTokenFromTag(message);
            if (loggedIn)
            {
                if (userToken == loggedInUser)
                {
                    WriteMessageText("Already logged in, logging out");
                    loggedIn = false;
                    loggedInUser = "";
                    gpiiLogout(userToken);
                }
                else // a different user wants to login, so we will log them out
                {    // first and then let the next person log in
                    WriteMessageText("Logged in with different user, will log them out first.");
                    gpiiLogout(loggedInUser);
                    System.Threading.Thread.Sleep(1000);
                    loggedIn = true;
                    loggedInUser = userToken;
                    gpiiLogin(userToken);
                }    
            }
            else
            {
                loggedIn = true;
                loggedInUser = userToken;
                gpiiLogin(userToken);
            }
        }

        public void ProximityDeviceArrived(Windows.Networking.Proximity.ProximityDevice device)
        {
            WriteMessageText("Proximate device arrived. id = " + device.DeviceId + "\n");
            
        }

        public void ProximityDeviceDeparted(Windows.Networking.Proximity.ProximityDevice device)
        {
            WriteMessageText("Proximate device departed. id = " + device.DeviceId + "\n");
        }

        public void WriteMessageText(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}