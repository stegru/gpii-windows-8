using System;
using System.IO;
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
            string lgsStartupDir = setupAppDataInstall();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                var applicationContext = new GPIIApplicationContext(new string[] {lgsStartupDir, "node.exe", "lgs_driver.js"});
                Application.Run(applicationContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Program Terminated Unexpectedly",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /**
         * Returns the full path to our user copy of the install in their AppData.
         */
        static string setupAppDataInstall()
        {   
            // TODO This needs to be dynamic, and trickily I *might* not be able to depend on the current
            // working directory.. although I might be able to.  Need to check if our install location is
            // actually getting put in the registry.
            //string lgsInstallDir = "Z:\\shared_with_win7\\code\\library-gpii-system\\lgs-station";
            string lgsInstallDir = "C:\\Program Files (x86)\\GPII\\lgs-station";
            
            string appData = Environment.GetEnvironmentVariable("APPDATA");
            bool gpiiAppDataDir = Directory.Exists(appData+"\\gpii");
            if (!gpiiAppDataDir)
            {
                Directory.CreateDirectory(appData + "\\gpii");
            }
            string lgsStationAppDataDir = appData+"\\gpii\\lgs-station";
            if (!Directory.Exists(lgsStationAppDataDir))
            {
                DirectoryCopy(lgsInstallDir, lgsStationAppDataDir, true);
            }
            return lgsStationAppDataDir;
        }

        // https://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                try { 
                    file.CopyTo(temppath, false);
                }
                catch (PathTooLongException ptle)
                {
                    // There are a few files in some grunt module that end up with incredibly deep nested modules.
                    // They all appear to be for testing.
                }
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        /*
        static void Main(string[] args)
        {
            System.Console.WriteLine("About to try NFC 3!");
            GPIINFCListener nfc = new GPIINFCListener();
            foreach (string reader in nfc.ListReaders()) {
                System.Console.WriteLine(reader);
            }

            nfc.SelectDevice();
            nfc.establishContext();
            nfc.getGPIIToken();
        }
         */
    }

}