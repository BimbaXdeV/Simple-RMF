using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class ConnectionCommand : InlineCommand
    {
        private readonly IServerSessionManager _sessionManager;

        public override string Category => "Performance";
        public override string Name => "conns";
        public override string Description => "Displays a list of active client sessions and their basic network stats";
        public override string[]? Parameters => null;

        public ConnectionCommand(
            IServerSessionManager sessionManager,
            ILogger<CommandHandler> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _sessionManager = sessionManager;
        }

        public override void Execute(string[] args)
        {
            IServerClientSession[] connections = _sessionManager.GetActiveConnections();
            if (!_sessionManager.ConnectionsExist)
            {
                CmLogger.LogInformation("No active connections...");
                return;
            }

            int maxAddr = 0;
            int maxPort = 0;
            int maxRecv = 0;
            int maxSent = 0;
            foreach (IServerClientSession c in connections)
            {
                maxAddr = Math.Max(maxAddr, c.RemoteEndPoint.Address.ToString().Length);
                maxPort = Math.Max(maxPort, c.RemoteEndPoint.Port.ToString().Length);
                maxRecv = Math.Max(maxRecv, c.TotalPacketsReceived.ToString().Length);
                maxSent = Math.Max(maxSent, c.TotalPacketsSent.ToString().Length);
            }
            int maxCount = connections.Length.ToString().Length;

            CmLogger.LogInformation("Active connections list:");
            for (int i = 0; i < connections.Length; i++)
            {
                IServerClientSession c = connections[i];

                string index = (i + 1).ToString().PadLeft(maxCount);
                string ipAddress = c.RemoteEndPoint.Address.ToString().PadLeft(maxAddr) ?? string.Empty;
                string port = c.RemoteEndPoint.Port.ToString().PadRight(maxPort) ?? string.Empty;
                string receivedPackets = c.TotalPacketsReceived.ToString().PadRight(maxRecv);
                string sentPackets = c.TotalPacketsSent.ToString().PadRight(maxSent);

                CmLogger.LogInformation(
                    "{Index}. {IpAddress}:{Port} | Recv: {ReceivedPackets}, Sent: {SentPackets}, Uptime: {Uptime}",
                    index, ipAddress, port, receivedPackets, sentPackets, c.LastTransferTime.ToLocalTime().ToString(RmfConstants.DateTimeFormatHms)
                );
            }
        }
    }
}
