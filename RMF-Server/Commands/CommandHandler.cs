using Avalonia.Media;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Network;
using RMF.Core.Packets.Server;
using RMF.Core.Screen;
using RMF.Core.Security;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Metrics;
using SkiaSharp;
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
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ICommandManager _commandManager;
        private readonly IServerMetricsMonitor _metrics;
        private readonly IAvaloniaManager _avaloniaManager;
        private readonly IServerSessionManager _sessionManager;
        private readonly ITlsManager _tlsManager;
        private readonly IFirewall _firewall;
        private readonly IThemeManager _themeManager;
        private readonly IConsoleExtensions _consoleExtensions;
        private readonly ILogger<CommandHandler> _logger;
        private readonly AppearanceConfig _appearanceConfig;
        private readonly StreamingConfig _streamingConfig;

        private const byte CommandSyntaxMaxLength = 28;

        private const byte CpuCoresMediumThreshold = 4;
        private const byte CpuCoresNormalThreshold = 8;
        private const byte CpuMediumLoadThreshold = 30;
        private const byte CpuHighLoadThreshold = 60;
        private const byte TotalRamGbMediumThreshold = 2;
        private const byte TotalRamGbNormalThreshold = 4;
        private const byte RamGbUsageMediumThreshold = 20;
        private const byte RamGbUsageHighThreshold = 40;

        public CommandHandler(
            IHostApplicationLifetime lifetime,
            ICommandManager commandManager,
            IServerMetricsMonitor metrics,
            IAvaloniaManager avaloniaManager,
            IServerSessionManager sessionManager,
            ITlsManager tlsManager,
            IFirewall firewall,
            IThemeManager themeManager,
            IConsoleExtensions consoleExtensions,
            ILogger<CommandHandler> logger,
            AppearanceConfig appearanceConfig,
            StreamingConfig streamingConfig
        )
        {
            this._lifetime = lifetime;
            this._commandManager = commandManager;
            this._metrics = metrics;
            this._avaloniaManager = avaloniaManager;
            this._sessionManager = sessionManager;
            this._tlsManager = tlsManager;
            this._firewall = firewall;
            this._themeManager = themeManager;
            this._consoleExtensions = consoleExtensions;
            this._logger = logger;
            this._appearanceConfig = appearanceConfig;
            this._streamingConfig = streamingConfig;
        }

        private bool Validator(string[] commandStructure, CommandParameter[]? parameters)
        {
            if (commandStructure.Length - 1 != parameters!.Length)
            {
                this._logger.LogError("The command parameter count mismatch. Expected: {ExpectedParameters}, but received: {ReceivedParameters}", parameters.Length, commandStructure.Length - 1);
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
                            this._logger.LogWarning("The parameter \"{ParameterName}\" expects an integer value, but received: \"{ReceivedParameter}\"", param.Name, inputParam);
                            return false;
                        }
                        break;

                    case "float":
                        if (!float.TryParse(inputParam, out _))
                        {
                            this._logger.LogWarning("The parameter \"{ParameterName}\" expects a float value, but received: \"{ReceivedParameter}\"", param.Name, inputParam);
                            return false;
                        }
                        break;

                    case "bool":
                        if (!bool.TryParse(inputParam, out _))
                        {
                            this._logger.LogWarning("The parameter \"{ParameterName}\" expects a boolean value (true/false), but received: \"{ReceivedParameter}\"", param.Name, inputParam);
                            return false;
                        }
                        break;

                    default:
                        this._logger.LogWarning("Unknown parameter type for \"{ParameterName}\"", param.Name);
                        return false;
                }
            }

            return true;
        }

        private static string GetStateColorKey(double currentValue, double secondThreshold, double thirdThreshold, bool reverse = false)
        {
            if (!reverse)
            {
                return currentValue switch
                {
                    _ when currentValue < secondThreshold => "MachineLoadNormal",
                    _ when currentValue < thirdThreshold => "MachineLoadMedium",
                    _ => "MachineLoadHigh"
                };
            }
            else
            {
                return currentValue switch
                {
                    _ when currentValue < secondThreshold => "MachineLoadHigh",
                    _ when currentValue < thirdThreshold => "MachineLoadMedium",
                    _ => "MachineLoadNormal"
                };
            }
        }

        public void SwitchHandle(string command)
        {
            // Here will be the command handling logic
        }

        public async Task SearchHandle(string input, Command command, CancellationToken token)
        {
            string[] inputCommandStructure = input.Split(' ');
            string commandName = inputCommandStructure[0];
            if (commandName != command.Name)
            {
                this._logger.LogError("Command name mismatch. Expected: \"{ExpectedCommandName}\", but received: \"{ReceivedCommandName}\"", command.Name, commandName);
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
                this._logger.LogError("No processor found for command \"{CommandName}\"", commandName);
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
                else if (p.ParameterType == typeof(CancellationToken))
                {
                    assembledParams[p.Position] = token;
                }
            }

            object? processResult = processMethod.Invoke(this, assembledParams);
            if (processResult is Task taskResult)
            {
                await taskResult;
            }
        }

        // All command processors
        private void Cmlst()
        {
            this._logger.LogInformation("Available inline commands:");
            if (this._commandManager.GetAllCommands().Count == 0)
            {
                this._logger.LogInformation("No commands have been loaded...");
                return;
            }

            List<Command> commands = this._commandManager.GetAllCommands();
            byte categoryIndex = 1;

            ThemeColor categoryIndexColor = this._themeManager.GetColor("CategoryIndex");
            ThemeColor commandColor = this._themeManager.GetColor("CommandName");
            ThemeColor paramColor = this._themeManager.GetColor("ParameterName");

            IEnumerable<IGrouping<string, Command>> groupedCommands = commands.GroupBy(c => c.Category);
            foreach (IGrouping<string, Command> group in groupedCommands)
            {
                string categoryName = !string.IsNullOrEmpty(group.Key)
                    ? group.Key
                    : "Uncategorized";

                this._logger.LogInformation(
                    "{IndexColorStart}[{Index}]{IndexColorEnd} {CategoryName}:",
                    categoryIndexColor,
                    categoryIndex,
                    ThemeColor.AnsiReset,
                    char.ToUpper(group.Key[0]) + group.Key.Substring(1)
                );

                ushort commandIndex = 1;

                foreach (Command cm in group)
                {
                    string visibleParams = cm.Parameters?.Length > 0
                        ? " " + string.Join(" ", cm.Parameters.Select(p => $"\"{p.Name}\""))
                :       string.Empty;
                    string visibleSyntax = $" {categoryIndex}.{commandIndex}. /{cm.Name}{visibleParams}";

                    string coloredParams = cm.Parameters?.Length > 0
                        ? $" {paramColor}{string.Join(" ", cm.Parameters.Select(p => $"\"{p.Name}\""))}{ThemeColor.AnsiReset}"
                        : string.Empty;
                    string coloredSyntax = $" {categoryIndex}.{commandIndex}. {commandColor}/{cm.Name}{ThemeColor.AnsiReset}{coloredParams}";

                    int paddingLength = Math.Max(0, CommandSyntaxMaxLength - visibleSyntax.Length);
                    string padding = new(' ', paddingLength);

                    string description = cm.Description ?? "Description is empty...";

                    this._logger.LogInformation(
                        "{CommandSyntax}{Padding} : {Description}",
                        coloredSyntax, padding, description
                    );
                    commandIndex++;
                }
                categoryIndex++;
            }
        }

        private void Conlst()
        {
            IServerClientSession[] connections = this._sessionManager.GetActiveConnections();
            if (!this._sessionManager.ConnectionsExist)
            {
                this._logger.LogInformation("No active connections...");
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

            this._logger.LogInformation("Active connections list:");
            for (int i = 0; i < connections.Length; i++)
            {
                IServerClientSession c = connections[i];

                string index = (i + 1).ToString().PadLeft(maxCount);
                string ipAddress = c.RemoteEndPoint.Address.ToString().PadLeft(maxAddr) ?? string.Empty;
                string port = c.RemoteEndPoint.Port.ToString().PadRight(maxPort) ?? string.Empty;
                string receivedPackets = c.TotalPacketsReceived.ToString().PadRight(maxRecv);
                string sentPackets = c.TotalPacketsSent.ToString().PadRight(maxSent);

                this._logger.LogInformation(
                    "{Index}. {IpAddress}:{Port} | Recv: {ReceivedPackets} | Sent: {SentPackets} | Last act: {LastTransferTime}",
                    index, ipAddress, port, receivedPackets, sentPackets, c.LastTransferTime.ToLocalTime().ToString(RmfConstants.TimeSpanFormatHms)
                );
            }
        }

        private void Banlst()
        {
            string[] bannedIPs = this._firewall.GetBannedIPs();
            if (bannedIPs.Length == 0)
            {
                this._logger.LogInformation("No banned IPs...");
                return;
            }

            this._logger.LogInformation("Banned IPs list:");
            int maxCounterLength = bannedIPs.Length.ToString().Length;
            int counter = 1;
            foreach (string ip in bannedIPs)
            {
                this._logger.LogInformation("{Index}. {IpAddress}", counter.ToString().PadLeft(maxCounterLength), ip);
                counter++;
            }
        }

        private void Clear()
        {
            this._consoleExtensions.ClearConsole(this._logger);
        }

        private void Shutdown(string input)
        {
            this._logger.LogInformation("The \"{CommandName}\" command received. Initiating shutdown process...", input);
            this._lifetime.StopApplication();
        }

        private void Status()
        {
            int cpuCores = this._metrics.CoresCount;
            double cpuLoad = this._metrics.GetCpuLoadPercentage();
            RmfRamUsage ramUsage = this._metrics.GetRamUsage();

            ThemeColor statusColor = this._themeManager.GetColor("ServerStatus");
            ThemeColor hintColor = this._themeManager.GetColor("ServerStatusHint");
            ThemeColor cpuLoadColor = this._themeManager.GetColor(GetStateColorKey(cpuLoad, CpuMediumLoadThreshold, CpuHighLoadThreshold));
            ThemeColor cpuCoresColor = this._themeManager.GetColor(GetStateColorKey(cpuCores, CpuCoresMediumThreshold, CpuCoresNormalThreshold, reverse: true));
            ThemeColor totalRamColor = this._themeManager.GetColor(GetStateColorKey(ramUsage.TotalMemoryGb, TotalRamGbMediumThreshold, TotalRamGbNormalThreshold, reverse: true));
            ThemeColor usedRamColor = this._themeManager.GetColor(GetStateColorKey(ramUsage.UsedMemoryGb, RamGbUsageMediumThreshold, RamGbUsageHighThreshold));

            this._logger.LogInformation("Server status ({StatusColorStart}Online{StatusColorEnd})", statusColor, ThemeColor.AnsiReset);
            this._logger.LogInformation("- Server uptime        : {Uptime}", (DateTime.Now - this._metrics.ProcessStartTime).ToString(RmfConstants.TimeSpanFormatHms));
            this._logger.LogInformation("- Active connections   : {ActiveConnections}  {HintColorStart}(more in: \"/conlst\"){HintColorEnd}",
                this._sessionManager.TotalConnections, hintColor, ThemeColor.AnsiReset
            );
            this._logger.LogInformation("- Connection IP filters: {BannedIPs}  {HintColorStart}(more in: \"/banlst\"){HintColorEnd}",
                this._firewall.GetBannedIPsCount(), hintColor, ThemeColor.AnsiReset
            );
            this._logger.LogInformation("- CPU total load       : {CpuColorStart}{CpuLoad:f2}%{CpuColorEnd}  ({CoresColorStart}{CoresCount}{CoresColorEnd} cores)",
                cpuLoadColor, cpuLoad, ThemeColor.AnsiReset, cpuCoresColor, cpuCores, ThemeColor.AnsiReset
            );
            this._logger.LogInformation("- Server RAM           : {TRamColorStart}{TotalRam:f3} GB{TRamColorEnd},  Used: {URamColorStart}{UsedRam:f3} GB{URamColorEnd}",
                totalRamColor, ramUsage.TotalMemoryGb, ThemeColor.AnsiReset, usedRamColor, ramUsage.UsedMemoryGb, ThemeColor.AnsiReset
            );
        }

        private void Certdata()
        {
            X509Certificate2 certificate = this._tlsManager.GetOrCreateCertificate();
            this._logger.LogInformation("Server TLS Certificate:");
            this._logger.LogInformation("- Subject    : {Subject}", certificate.Subject);
            this._logger.LogInformation("- Issuer     : {Issuer}", certificate.Issuer);
            this._logger.LogInformation("- Signature  : {Algorithm}", certificate.SignatureAlgorithm.FriendlyName);
            this._logger.LogInformation("- Version    : v{Version}", certificate.Version);
            this._logger.LogInformation("- Expiration : {StartTime} - {ExpirationTime}",
                certificate.NotBefore.ToString(RmfConstants.DateTimeFormatYmdHms), certificate.NotAfter.ToString(RmfConstants.DateTimeFormatYmdHms)
            );
            this._logger.LogInformation("- Fingerprint: {Fingerprint} ", certificate.Thumbprint);
        }

        private void Ver()
        {
            string? serverVersion = RmfVersion.App?.ToString(3);
            string? coreVersion = RmfVersion.Core?.ToString(3);

            if (serverVersion != null && coreVersion != null)
            {
                this._logger.LogInformation("Assembly versions:");
                this._logger.LogInformation("{ServerName}: {ServerVersion}", this._appearanceConfig.AppTitle, serverVersion);
                this._logger.LogInformation("RMF.Core: {CoreVersion}", coreVersion);
            }
            else
            {
                this._logger.LogWarning("Version information is not available now");
            }
        }

        private void Screen(string input)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (this._sessionManager.GetClientSession(targetEndPoint, out IServerClientSession? session))
            {
                ScreenshotRequest screenshotRequest = new()
                {
                    FormatID = (byte)this._streamingConfig.ScreenshotFrameFormat,
                    QualityPercent = (byte)this._streamingConfig.ScreenshotQualityPercentage
                };
                session!.SendPacket(screenshotRequest);
                this._logger.LogInformation("Successfully sent to {EndPoint}, waiting for remote screenshot...", targetEndPoint);
            }
            else
            {
                this._logger.LogError("No connection found named \"{EndPoint}\"", targetEndPoint);
            }
        }

        private async Task Stream(string input)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (this._sessionManager.GetClientSession(targetEndPoint, out IServerClientSession? session))
            {
                StreamingRequest streamingRequest = new()
                {
                    IsActive = true,
                    FormatID = (byte)this._streamingConfig.StreamingFrameFormat,
                    Quality = (byte)this._streamingConfig.StreamingQualityPercentage,
                    FrameUpdateRate = this._streamingConfig.StreamingFrameUpdateRate,
                    TargetFPS = (short)this._streamingConfig.StreamingTargetFPS
                };
                session!.SendPacket(streamingRequest);

                this._avaloniaManager.StreamingClientEndPoint = session.RemoteEndPoint;
                await this._avaloniaManager.ShowWindow();
                this._avaloniaManager.SetWindowTitle(this._appearanceConfig.WindowTitle + " | " + targetEndPoint);
                this._logger.LogInformation("Streaming session started with {EndPoint}", session.RemoteEndPoint);
            }
            else
            {
                this._logger.LogError("No connection found named \"{EndPoint}\"", targetEndPoint);
            }
        }

        private async Task Dstream()
        {
            try
            {   
                IPEndPoint? ipEndPoint = this._avaloniaManager.StreamingClientEndPoint;
                if (ipEndPoint == null)
                {
                    this._logger.LogInformation("No active stream to stop...");
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
                    this._logger.LogInformation("Successfully sent to {EndPoint}, waiting for stopping stream...", endPoint);
                }
                else
                {
                    this._logger.LogError("No connection found named \"{EndPoint}\"", endPoint);
                }
            }
            finally
            {
                this._avaloniaManager.SetWindowTitle(this._appearanceConfig.WindowTitle);
                await this._avaloniaManager.HideWindow();
            }
        }
    }
}
