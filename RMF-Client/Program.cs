using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Packets;
using RMF_Client.DI;
using RMF_Client.Logic;
using RMF_Client.Monitors;
using RMF_Client.Network;

namespace RMF_Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            RmfClientHost client = new(args);
            await client.RunAsync();
        }
    }
}
