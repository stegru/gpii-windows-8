using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GPII
{
    class GPIINodeManager
    {
        public Process localGPIIProcess = null;

        public String lgsWorkingDirectory = "C:\\Program Files (x86)\\gpii\\windows";
        public String lgsFileName = "grunt";
        public String lgsArguments = "start";

        public void startLocalGPII()
        {
            ProcessStartInfo gpiiStartInfo = new ProcessStartInfo();
            gpiiStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            gpiiStartInfo.WorkingDirectory = lgsWorkingDirectory;
            gpiiStartInfo.FileName = lgsFileName;
            gpiiStartInfo.Arguments = lgsArguments;
            localGPIIProcess = Process.Start(gpiiStartInfo);
        }
    }
}
