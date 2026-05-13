using RMF.Core.Packets.Server;
using RMF_Server.Channels;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal static class LifecycleController
    {
        public static bool IsBaseInitialized = false;
        public static CancellationTokenSource? Input { get; private set; }  // Master token source, it starts the whole chain of shutdown
        public static CancellationTokenSource? Server { get; private set; }

        public static bool IsFinalInitialized = false;
        public static CancellationTokenSource? Output { get; private set; }

        public static void Initialize()
        {
            if (IsBaseInitialized && IsFinalInitialized)
            {
                return;
            }

            PropertyInfo[] tokenSources = typeof(LifecycleController).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(CancellationTokenSource))
                .ToArray();

            foreach (PropertyInfo property in tokenSources)
            {
                CancellationTokenSource cts = new();
                property.SetValue(null, cts);
            }
            IsBaseInitialized = true;
            IsFinalInitialized = true;
        }

        private static async Task WaitForParting(int timeoutSecs)
        {
            Logging.Output("The server is parting...");
            DateTime deadline = DateTime.Now + TimeSpan.FromSeconds(timeoutSecs);
            while (SessionManager.Connections.IsEmpty == false && DateTime.Now < deadline)
            {
                await Task.Delay(100);
            }

            if (SessionManager.Connections.IsEmpty == false)
            {
                Logging.Warning($"The server parting timeout has expired, {SessionManager.Connections.Count} clients are still connected");
            }
            Logging.Output("The server successfully parted");
        }

        public static async Task BaseShutdown()
        {
            if (!IsBaseInitialized)
            {
                Logging.Warning("The server lifecycle is not initialized, shutdown is not required");
                return;
            }

            Logging.Separator();
            Logging.Warning("Cancellation requested, stopping server...");

            Input!.Cancel();

            if (ConfigurationManager.EnableRelativeParting)
            {
                EndOfEventsPacket endOfEventsPacket = new();
                SessionManager.BroadcastPacket(endOfEventsPacket, CancellationToken.None);
                await WaitForParting(ConfigurationManager.PartingTimeoutSecs);
            }

            Server!.Cancel();
            SessionManager.ClearConnections();
            await ChannelDispatcher.CloseChannels();

            IsBaseInitialized = false;
        }

        public static void FinalShutdown(bool cleanupSources = false)
        {
            if (!IsFinalInitialized)
            {
                Logging.Warning("The final lifecycle is not initialized, shutdown is not required");
                return;
            }

            Output!.Cancel();
            if (cleanupSources)
            {
                DisposeAll();
            }
            IsFinalInitialized = false;
        }

        public static void DisposeAll()
        {
            PropertyInfo[] tokenSources = typeof(LifecycleController).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(CancellationTokenSource))
                .ToArray();

            int disposedSourcesCount = 0;
            int totalTokenSources = tokenSources.Length;
            foreach (PropertyInfo property in tokenSources)
            {
                CancellationTokenSource? cts = property.GetValue(null) as CancellationTokenSource;
                if (cts != null && cts is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                property.SetValue(null, null);
                disposedSourcesCount++;
            }
            Logging.Output($"During the token cleanup, {disposedSourcesCount} / {totalTokenSources} active sources were cleared successfully");
        }
    }
}
