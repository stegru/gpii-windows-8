using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using Windows.Networking.Proximity;

namespace GPII
{
    class GPIIProximityListener
    {
        ProximityDevice proximityDevice;
        bool loggedIn = false;
        string loggedInUser = "";
        DateTime lastTapTime = DateTime.UtcNow;
        public GPIIApplicationContext applicationContext;

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
