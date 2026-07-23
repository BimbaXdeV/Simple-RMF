using Avalonia.Media;
using RMF.Core.Bases;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF.Core.Packets.Server;
using RMF.Core.Screen;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.Interfaces;
using RMF_Server.Logic;
using RMF_Server.Storage;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class CommandHandler : ICommandHandler
    {
        private readonly ICommandManager _commandManager;
        private readonly IServerSessionManager _sessionManager;
        private readonly ITlsManager _tlsManager;
        private readonly IFirewall? _firewall;
        private readonly ILoggingEngine? _logger;
        private readonly AppearanceConfig? _appearanceConfig;
        private readonly StreamingConfig? _streamingConfig;

        public CommandHandler(
            ICommandManager commandManager,
            IServerSessionManager sessionManager,
            ITlsManager tlsManager,
            IFirewall? firewall = null,
            ILoggingEngine? logger = null,
            AppearanceConfig? appearanceConfig = null,
            StreamingConfig? streamingConfig = null
        )
        {
            this._commandManager = commandManager;
            this._sessionManager = sessionManager;
            this._tlsManager = tlsManager;
            this._firewall = firewall;
            this._logger = logger;
            this._appearanceConfig = appearanceConfig;
            this._streamingConfig = streamingConfig;
        }

        private bool Validator(string[] commandStructure, CommandParameter[]? parameters)
        {
            if (commandStructure.Length - 1 != parameters!.Length)
            {
                this._logger?.Warning($"The command parameter count mismatch. Expected: {parameters.Length}, but received: {commandStructure.Length - 1}");
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                CommandParameter param = parameters[i];
                string inputParam = commandStructure[i + 1];

                switch (param.Type)
                {
                    case "string":
                        // No specific validation needed for strings
                        break;

                    case "int":
                        if (!int.TryParse(inputParam, out _))
                        {
                            this._logger?.Warning($"The parameter \"{param.Name}\" expects an integer value, but received: \"{inputParam}\"");
                            return false;
                        }
                        break;

                    case "float":
                        if (!float.TryParse(inputParam, out _))
                        {
                            this._logger?.Warning($"The parameter \"{param.Name}\" expects a float value, but received: \"{inputParam}\"");
                            return false;
                        }
                        break;

                    case "bool":
                        if (!bool.TryParse(inputParam, out _))
                        {
                            this._logger?.Warning($"The parameter \"{param.Name}\" expects a boolean value (true/false), but received: \"{inputParam}\"");
                            return false;
                        }
                        break;

                    default:
                        this._logger?.Warning($"Unknown parameter type for \"{param.Name}\"");
                        return false;
                }
            }

            return true;
        }

        public void SwitchHandle(string command)
        {
            // Here will be the command handling logic
        }

        public async Task SearchHandle(string input, Command command, CancellationTokenSource cts)
        {
            string[] inputCommandStructure = input.Split(' ');
            string commandName = inputCommandStructure[0];
            if (commandName != command.Name)
            {
                this._logger?.Warning($"Command name mismatch. Expected: \"{command.Name}\", but received: \"{commandName}\"");
                return;
            }

            // All exceptions are handled in the validator with logging warnings
            if (!Validator(inputCommandStructure, command.Parameters))
            {
                return;
            }

            string processMethodName = char.ToUpper(commandName[0]) + commandName.Substring(1);
            Type type = typeof(CommandHandler);
            MethodInfo? processMethod = type.GetMethod(processMethodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (processMethod == null)
            {
                this._logger?.Warning($"No processor found for command \"{commandName}\"", toHistory: false);
                return;
            }


            ParameterInfo[] processParams = processMethod.GetParameters();
            object[] assembledParams = new object[processParams.Length];
            foreach (ParameterInfo p in processParams)
            {
                if (p.ParameterType == typeof(string))
                {
                    assembledParams[p.Position] = input;
                }
                else if (p.ParameterType == typeof(CancellationTokenSource))
                {
                    assembledParams[p.Position] = cts;
                }
            }

            object? processResult = processMethod.Invoke(null, assembledParams);
            if (processResult is Task taskResult)
            {
                await taskResult;
            }
        }

        // All command processors
        private void Cmlst()
        {
            this._logger?.Message("* Available inline commands:");
            if (this._commandManager.GetAllCommands().Count == 0)
            {
                this._logger?.Message("No commands have been loaded...");
                return;
            }

            foreach (Command cm in this._commandManager.GetAllCommands())
            {
                byte[] paramColor = ThemeManager.ParameterName;
                string parametersNamesPerformance = cm.Parameters != null ? Colorist.ColoredFilterRGB(paramColor[0], paramColor[1], paramColor[2]) + string.Join(" ", cm.Parameters.Select(p => $"\"{p.Name}\"")) + Colorist.ResetColor() : "";
                string descriptionPerformance = cm.Description ?? "Description is empty...";
                
                byte[] commandColor = ThemeManager.CommandName;
                this._logger?.Message($"{Colorist.ColoredFilterRGB(commandColor[0], commandColor[1], commandColor[2])}- {cm.Name}{Colorist.ResetColor()} {parametersNamesPerformance} - {descriptionPerformance}");
            }
        }

        private void Conlst()
        {
            IServerClientSession[] connections = this._sessionManager.GetActiveConnections();
            if (!this._sessionManager.ConnectionsExist)
            {
                this._logger?.Message("No active connections...", toHistory: false);
                return;
            }

            int maxAddr = 0;
            int maxPort = 0;
            int maxRecv = 0;
            int maxSent = 0;
            foreach (ServerClientSession c in connections)
            {
                maxAddr = Math.Max(maxAddr, c.RemoteEndPoint.Address.ToString().Length);
                maxPort = Math.Max(maxPort, c.RemoteEndPoint.Port.ToString().Length);
                maxRecv = Math.Max(maxRecv, c.TotalPacketsReceived.ToString().Length);
                maxSent = Math.Max(maxSent, c.TotalPacketsSent.ToString().Length);
            }
            int maxCount = connections.Length.ToString().Length;

            this._logger?.Message("* Active connections list:");
            for (int i = 0; i < connections.Length; i++)
            {
                IServerClientSession c = connections[i];

                string index = (i + 1).ToString().PadLeft(maxCount);
                string ipAddress = c.RemoteEndPoint.Address.ToString().PadLeft(maxAddr) ?? string.Empty;
                string port = c.RemoteEndPoint.Port.ToString().PadRight(maxPort) ?? string.Empty;
                string receivedPackets = c.TotalPacketsReceived.ToString().PadRight(maxRecv);
                string sentPackets = c.TotalPacketsSent.ToString().PadRight(maxSent);

                this._logger?.Message($"{index}. {ipAddress}:{port} | Recv: {receivedPackets} | Sent: {sentPackets} | Last act: {c.LastTransferTime.ToLocalTime():HH:mm:ss}");
            }
        }

        private void Banlst()
        {
            if (this._firewall == null)
            {
                this._logger?.Warning("Firewall is not initialized in current instance, unable to access the client blacklist");
                return;
            }

            string[] bannedIPs = this._firewall.GetBannedIPs();
            if (bannedIPs.Length == 0)
            {
                this._logger?.Message("No banned IPs...", toHistory: false);
                return;
            }

            this._logger?.Message("* Banned IPs list:");
            int maxCounterLength = bannedIPs.Length.ToString().Length;
            int counter = 1;
            foreach (string ip in bannedIPs)
            {
                this._logger?.Message($"{string.Format($"{{0,{maxCounterLength}}}", counter.ToString())}. {ip}");
                counter++;
            }
        }

        private void Clear()
        {
            this._logger?.ClearConsole();
        }

        private void Shutdown(string input, CancellationTokenSource cts)
        {
            this._logger?.Output($"The \"{input}\" command received. Initiating shutdown process...");
            cts.Cancel();
        }

        private void Certdata()
        {
            X509Certificate2 certificate = this._tlsManager.GetOrCreateCertificate();
            this._logger?.Message(
                "* Server TLS Certificate:" + Environment.NewLine +
                "- Subject    : " + certificate.Subject + Environment.NewLine +
                "- Issuer     : " + certificate.Issuer + Environment.NewLine +
                "- Expiration : " + certificate.NotAfter + Environment.NewLine +
                "- Fingerprint: " + certificate.Thumbprint
            );
        }

        private void Ver()
        {
            string? serverVersion = RMFVersion.App?.ToString(3);
            string? coreVersion = RMFVersion.Core?.ToString(3);

            if (serverVersion != null && coreVersion != null)
            {
                this._logger?.Message($"* Assembly versions{Environment.NewLine}{this._appearanceConfig?.AppTitle ?? "Server"}: {serverVersion}{Environment.NewLine}Core: {coreVersion}");
            }
            else
            {
                this._logger?.Message("Version information is not available now");
            }
        }

        private void Screen(string input)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (this._sessionManager.GetClientSession(targetEndPoint, out IServerClientSession? session) && session != null)
            {
                ScreenshotRequest screenshotRequest = new()
                {
                    FormatID = (byte)(this._streamingConfig?.ScreenshotFrameFormat ?? default),
                    QualityPercent = (byte)(this._streamingConfig?.ScreenshotQualityPercentage ?? 100)
                };
                session.SendPacket(screenshotRequest);
                this._logger?.Message($"Successfully sent to {targetEndPoint}, waiting for remote screenshot...");
            }
            else
            {
                this._logger?.Message($"No connection found named \"{targetEndPoint}\"", toHistory: false);
            }
        }

        private async Task Stream(string input)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (this._sessionManager.GetClientSession(targetEndPoint, out IServerClientSession? session) && session != null)
            {
                StreamingRequest streamingRequest = new()
                {
                    IsActive = true,
                    FormatID = (byte)(this._streamingConfig?.StreamingFrameFormat ?? default),
                    Quality = (byte)(this._streamingConfig?.StreamingQualityPercentage ?? 100),
                    FrameUpdateRate = this._streamingConfig?.StreamingFrameUpdateRate ?? 0,
                    TargetFPS = (short)(this._streamingConfig?.StreamingTargetFPS ?? 30)
                };
                session.SendPacket(streamingRequest);

                WindowManager.StreamingClientEndPoint = session.RemoteEndPoint;
                await WindowManager.ShowWindow();
                WindowManager.SetWindowTitle(this._appearanceConfig != null
                    ? this._appearanceConfig?.WindowTitle + " | " + targetEndPoint
                    : targetEndPoint
                );
                this._logger?.Output($"Streaming session started with {session.RemoteEndPoint}");
            }
            else
            {
                this._logger?.Message($"No connection found named \"{targetEndPoint}\"", toHistory: false);
            }
        }

        private async Task Dstream()
        {
            try
            {   
                IPEndPoint? ipEndPoint = WindowManager.StreamingClientEndPoint;
                if (ipEndPoint == null)
                {
                    this._logger?.Message("No active stream to stop...", toHistory: false);
                    return;
                }
                
                string endPoint = ipEndPoint.ToString();
                if (this._sessionManager.GetClientSession(endPoint, out IServerClientSession? session) && session != null)
                {
                    StreamingRequest streamingRequest = new()
                    {
                        IsActive = false
                    };
                    session.SendPacket(streamingRequest);
                    this._logger?.Message($"* Successfully sent to {endPoint}, waiting for stopping stream...");
                }
                else
                {
                    this._logger?.Message($"No connection found named \"{endPoint}\"", toHistory: false);
                }
            }
            finally
            {
                WindowManager.SetWindowTitle(this._appearanceConfig?.WindowTitle ?? "Disabled");
                await WindowManager.HideWindow();
            }
        }
    }
}
