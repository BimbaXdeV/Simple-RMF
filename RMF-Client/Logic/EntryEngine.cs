using Microsoft.Extensions.Hosting;
using RMF.Core.Appearance;
using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF.Core.Security;
using RMF_Client.Appearance;
using RMF_Client.Configurations;
using RMF_Client.Network;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal class EntryEngine : BackgroundService
    {
        private readonly IClientSessionManager _sessionManager;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IPacketFactory _packetFactory;
        private readonly IClientPacketProcessor _packetProcessor;
        private readonly IWindowManager _windowManager;
        private readonly IToolbarManager _toolBarManager;
        private readonly ConnectionConfig _connectionConfig;
        private readonly SecurityConfig _securityConfig;

        public EntryEngine(
            IClientSessionManager sessionManager,
            IConnectionFactory connectionFactory,
            IPacketFactory packetFactory,
            IClientPacketProcessor packetProcessor,
            IWindowManager windowManager,
            IToolbarManager toolBarManager,
            ConnectionConfig connectionConfig,
            SecurityConfig securityConfig
        )
        {
            this._sessionManager = sessionManager;
            this._connectionFactory = connectionFactory;
            this._packetFactory = packetFactory;
            this._packetProcessor = packetProcessor;
            this._windowManager = windowManager;
            this._toolBarManager = toolBarManager;
            this._connectionConfig = connectionConfig;
            this._securityConfig = securityConfig;
        }

        private async Task PacketListener(CancellationToken token)
        {
            this._windowManager.UpdateTitleStatus("Connected");

            if (!this._sessionManager.IsConnected)
            {
                return;
            }

            IConnectionClientSession session = this._sessionManager.GetRunningSession()!;
            while (this._sessionManager.IsConnected)
            {
                PacketHeader header = await session.ReadHeaderAsync(token);
                byte[] payload = await session.ReadPayloadAsync(header.Length, token);

                Packet? packet = null;
                try
                {
                    packet = this._packetFactory.CreatePacket(header.Id);
                    if (packet == null)
                    {
                        continue;
                    }

                    session.IncrementReceivedPackets();

                    ReadOnlySpan<byte> payloadSpan = payload.AsSpan(0, header.Length);
                    SpanReader payloadReader = new(payloadSpan);

                    packet.Deserialize(ref payloadReader);
                    this._packetProcessor.SwitchHandle(packet);  // When scaling, a new case needs to be added
                }
                catch (Exception)
                {
                    // Then, in place of all these stubs, I`ll put a log buffer to write them to a file
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(payload);
                    if (packet is IReleasable releasable)
                    {
                        releasable.Release();
                    }
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            int connectionAttempt = 1;
            while (!token.IsCancellationRequested)
            {
                this._windowManager.UpdateTitleStatus("Waiting for server...");

                try
                {
                    using INetworkConnection rawConnection = this._connectionFactory.CreateConnection();

                    this._windowManager.UpdateTitleStatus("Securing connection...");

                    using SslStream sslStream = new(
                        rawConnection.GetNetworkStream(),
                        false,
                        new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) =>
                        {
                            if (certificate == null)
                            {
                                return false;
                            }

                            // To synchronize the client and server TLS, pull fingerprint from the server using the "/certdata" command,
                            // and then place it in the client configuration (~/Resources/config.xml)
                            string actualFingerprint = certificate.GetCertHashString();
                            string expectedFingerprint = this._securityConfig.CertificateFingerprint?.Replace(" ", "").ToUpper() ?? string.Empty;
                            if (actualFingerprint != expectedFingerprint)
                            {
                                return false;
                            }

                            return true;
                        })
                    );

                    await sslStream.AuthenticateAsClientAsync(string.Empty).WaitAsync(TimeSpan.FromSeconds(_securityConfig.TlsHandshakeTimeoutSecs), token);
                    SecureConnectionAdapter tlsConnection = new(rawConnection, sslStream);
                    this._sessionManager.StartSession(tlsConnection);

                    IPEndPoint remoteEndPoint = rawConnection.RemoteEndPoint;
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointIP", remoteEndPoint.Address.ToString() },
                        { "endpointPort", remoteEndPoint.Port.ToString() }
                    });

                    await PacketListener(token);
                }

                catch (EndOfStreamException)
                {
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Server has closed the connection" }
                    });
                }

                catch (OperationCanceledException)
                {
                    this._windowManager.UpdateTitleStatus("Cancellation...");
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Cancellation requested, cleaning up the process..." }
                    });
                }

                catch (SocketException)
                {
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Failed to connect to " + this._connectionConfig.IPAddress + ":" + this._connectionConfig.Port }
                    });
                }

                catch (AuthenticationException)
                {
                    this._windowManager.UpdateTitleStatus("TLS handshake failed");
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Failed to accept server TLS handshake" }
                    });
                }

                catch (Exception ex)
                {
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "A client error occured: " + ex }
                    });
                }
                finally
                {
                    this._sessionManager.StopSession();
                    this._windowManager.UpdateTitleStatus("Finished");
                }

                if (this._connectionConfig.ConnectionRequestIntervalSecs <= 0)
                {
                    break;
                }

                connectionAttempt++;
                this._windowManager.UpdateTitleStatus("Attempting to reconnect... (" + connectionAttempt + ")");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(this._connectionConfig.ConnectionRequestIntervalSecs), token);
                }
                catch (OperationCanceledException)
                {
                    this._windowManager.UpdateTitleStatus("Cancellation...");
                    this._toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Cancellation requested, cleaning up the process..." }
                    });
                    break;
                }
            }
        }
    }
}
