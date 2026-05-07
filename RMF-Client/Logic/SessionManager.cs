using RMF_Client.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal static class SessionManager
    {
        public static ConnectionClientSession? Connection { get; private set; }

        public static void StartSession(TcpClient client, CancellationToken token)
        {
            try
            {
                Connection = new ConnectionClientSession(client, ConfigurationManager.ChannelPacketsCapacity, token);
            }
            catch (Exception)
            {
            }
        }

        public static void ClearSession()
        {
            Connection?.StopProcessing();
            Connection = null;
        }
    }
}
