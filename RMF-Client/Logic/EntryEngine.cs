using Microsoft.Extensions.Hosting;
using RMF.Core.Events;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF.Core.Network;
using RMF.Core.Packets;
using RMF_Client.Appearance;
using RMF_Client.Configurations;
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
        private readonly IPacketFactory _packetFactory;
        private readonly IClientPacketProcessor _packetProcessor;
        private readonly IWindowManager _windowManager;
        private readonly IToolbarManager _toolBarManager;
        private readonly ConnectionConfig _connectionConfig;
        private readonly SecurityConfig _securityConfig;

        public EntryEngine(
            IClientSessionManager sessionManager,
            IPacketFactory packetFactory,
            IClientPacketProcessor packetProcessor,
            IWindowManager windowManager,
            IToolbarManager toolBarManager,
            ConnectionConfig connectionConfig,
            SecurityConfig securityConfig
        )
        {
            _sessionManager = sessionManager;
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

                Packet? packet = null;
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
                _windowManager.UpdateTitleStatus("Waiting for server...");

                IPAddress ip = _connectionConfig.IPAddress != "Any"
                    ? IPAddress.Parse(_connectionConfig.IPAddress ?? "127.0.0.1")
                    : IPAddress.Any;

                int port = _connectionConfig.Port >= IPEndPoint.MinPort && _connectionConfig.Port <= IPEndPoint.MaxPort
                    ? _connectionConfig.Port
                    : 8000;  // Default port if the provided port is invalid

                try
                {
                    using TcpClient tcpClient = new(ip.ToString(), port);
                    using TcpConnection tcpConnection = new(tcpClient);

                    _windowManager.UpdateTitleStatus("Securing connection...");

                    using SslStream sslStream = new(
                        tcpConnection.GetNetworkStream(),
                        false,
                        new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) =>
                        {
                            if (certificate == null)
                            {
                                return false;
                            }

                            // To synchronize the client and server TLS, pull fingerprint from the server using the "/certdata" command,
                            // and then place it in the client configuration (~/Storage/config.xml)
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
                    SecureConnectionAdapter tlsConnection = new(tcpConnection, sslStream);
                    _sessionManager.StartSession(tlsConnection);

                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointIP", ip.ToString() },
                        { "endpointPort", port.ToString() }
                    });

                    await PacketListener(token);
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

                catch (SocketException)
                {
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Failed to connect to " + ip.ToString() + ":" + port.ToString() }
                    });
                }

                catch (AuthenticationException)
                {
                    _windowManager.UpdateTitleStatus("TLS handshake failed");
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Failed to accept server TLS handshake" }
                    });
                }

                catch (Exception ex)
                {
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "A client error occured: " + ex }
                    });
                }
                finally
                {
                    _sessionManager.StopSession();
                    _windowManager.UpdateTitleStatus("Finished");
                }

                if (_connectionConfig.ConnectionRequestIntervalSecs <= 0)
                {
                    break;
                }

                connectionAttempt++;
                _windowManager.UpdateTitleStatus("Attempting to reconnect... (" + connectionAttempt + ")");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_connectionConfig.ConnectionRequestIntervalSecs), token);
                }
                catch (OperationCanceledException)
                {
                    _windowManager.UpdateTitleStatus("Cancellation...");
                    _toolBarManager.ReplaceToolbarContent(new Dictionary<string, string>
                    {
                        { "endpointTime", "Cancellation requested, cleaning up the process..." }
                    });
                    break;
                }
            }
        }
    }
}
