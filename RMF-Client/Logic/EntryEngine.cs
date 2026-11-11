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
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IClientSessionManager _sessionManager;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IPacketFactory _packetFactory;
        private readonly IClientPacketProcessor _packetProcessor;
        private readonly IWindowManager _windowManager;
        private readonly IToolbarManager _toolBarManager;
        private readonly ConnectionConfig _connectionConfig;
        private readonly SecurityConfig _securityConfig;

        public EntryEngine(
            IHostApplicationLifetime lifetime,
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
            _lifetime = lifetime;
            _sessionManager = sessionManager;
            _connectionFactory = connectionFactory;
            _packetFactory = packetFactory;
            _packetProcessor = packetProcessor;
            _windowManager = windowManager;
            _toolBarManager = toolBarManager;
            _connectionConfig = connectionConfig;
            _securityConfig = securityConfig;
        }

        private async Task PacketListener(CancellationToken token)
        {
            _windowManager.UpdateTitleStatus("Connected");

            if (!_sessionManager.IsConnected)
            {
                return;
            }

            IConnectionClientSession session = _sessionManager.GetRunningSession()!;
            while (_sessionManager.IsConnected)
            {
                PacketHeader header = await session.ReadHeaderAsync(token);
                byte[] payload = await session.ReadPayloadAsync(header.Length, token);

                IPacket? packet = null;
                try
                {
                    packet = _packetFactory.CreatePacket(header.Id);
                    if (packet == null)
                    {
                        continue;
                    }

                    session.IncrementReceivedPackets();

                    ReadOnlySpan<byte> payloadSpan = payload.AsSpan(0, header.Length);
                    SpanReader payloadReader = new(payloadSpan);

                    packet.Deserialize(ref payloadReader);
                    _packetProcessor.SwitchHandle(packet);  // When scaling, a new case needs to be added
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
                bool isFirstAttempt = connectionAttempt == 1;
                string statusMessage = isFirstAttempt
                    ? "Waiting for server... (" + connectionAttempt + ")"
                    : "Retrying... (" + connectionAttempt + ")";
                _windowManager.UpdateTitleStatus(statusMessage);

                if (!isFirstAttempt)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_connectionConfig.ConnectionRequestDelaySecs), token);
                }

                try
                {
                    using INetworkConnection rawConnection = _connectionFactory.CreateConnection();

                    _windowManager.UpdateTitleStatus("Securing connection...");

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
                            string expectedFingerprint = _securityConfig.CertificateFingerprint?.Replace(" ", "").ToUpper() ?? string.Empty;
                            if (actualFingerprint != expectedFingerprint)
                            {
                                return false;
                            }

                            return true;
                        })
                    );

                    await sslStream.AuthenticateAsClientAsync(string.Empty).WaitAsync(TimeSpan.FromSeconds(_securityConfig.TlsHandshakeTimeoutSecs), token);
                    SecureConnectionAdapter tlsConnection = new(rawConnection, sslStream);
                    _sessionManager.StartSession(tlsConnection);

                    IPEndPoint remoteEndPoint = rawConnection.RemoteEndPoint;
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointIP", remoteEndPoint.Address.ToString() },
                        { "endpointPort", remoteEndPoint.Port.ToString() }
                    });

                    await PacketListener(token);
                }

                catch (SocketException)
                {
                    if ( connectionAttempt >= _connectionConfig.MaxConnectionAttempts)
                    {
                        _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                        {
                            { "endpointTime", "Max connection attempts reached. Exiting..." }
                        });
                        _lifetime.StopApplication();
                    }

                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Failed to connect to " + _connectionConfig.IPAddress + ":" + _connectionConfig.Port + " in the " + connectionAttempt + " attempt" }
                    });

                    connectionAttempt++;
                }

                catch (EndOfStreamException)
                {
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Server has closed the connection" }
                    });
                }

                catch (OperationCanceledException)
                {
                    _windowManager.UpdateTitleStatus("Cancellation...");
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Cancellation requested, cleaning up the process..." }
                    });
                }

                catch (AuthenticationException)
                {
                    _windowManager.UpdateTitleStatus("TLS failure");
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Failed to accept server TLS handshake with server at " + _connectionConfig.IPAddress + ":" + _connectionConfig.Port }
                    });
                }

                catch (Exception ex)
                {
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "A client error occured: " + ex.Message }
                    });
                }
                finally
                {
                    _sessionManager.StopSession();
                }
            }
        }
    }
}
