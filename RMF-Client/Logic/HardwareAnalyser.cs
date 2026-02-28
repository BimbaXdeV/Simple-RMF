using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal static class HardwareAnalyser
    {
        public static string GetMachineName()
        {
            return Environment.MachineName;
        }

        public static string GetUsername()
        {
            return Environment.UserName;
        }

        public static string GetOS()
        {
            return RuntimeInformation.OSDescription;
        }

        public static string GetArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
        }
    }
}
