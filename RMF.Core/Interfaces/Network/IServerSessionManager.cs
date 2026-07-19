using RMF.Core.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces.Network
{
    public interface IServerSessionManager
    {
        bool ConnectionsExist { get; }
        int TotalConnections { get; }

        void BroadcastPacket(Packet packet, CancellationToken token);
        IServerClientSession? NewConnection(INetworkConnection connection, CancellationToken token);
        bool GetClientSession(string endPoint, out IServerClientSession? session);
        Guid? GetSessionID(string endPoint);
        IServerClientSession[] GetActiveConnections();
        int GetConnectionsFromIP(IPAddress ip);
        void Disconnect(string endPoint);
        void ClearConnections();
    }
}
