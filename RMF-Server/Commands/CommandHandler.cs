using Avalonia.Media;
using RMF.Core.Bases;
using RMF.Core.Packets.Server;
using RMF.Core.Screen;
using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Storage;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class CommandHandler
    {
        private static readonly byte[] CommandNameRGB = { 180, 255, 229 };
        private static readonly byte[] ParameterNameRGB = { 123, 209, 179 };

        private static bool Validator(string[] commandStructure, CommandParameter[]? parameters)
        {
            if (commandStructure.Length - 1 != parameters!.Length)
            {
                Logging.Warning($"The command parameter count mismatch. Expected: {parameters.Length}, but received: {commandStructure.Length - 1}");
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
                            Logging.Warning($"The parameter \"{param.Name}\" expects an integer value, but received: \"{inputParam}\"");
                            return false;
                        }
                        break;

                    case "float":
                        if (!float.TryParse(inputParam, out _))
                        {
                            Logging.Warning($"The parameter \"{param.Name}\" expects a float value, but received: \"{inputParam}\"");
                            return false;
                        }
                        break;

                    case "bool":
                        if (!bool.TryParse(inputParam, out _))
                        {
                            Logging.Warning($"The parameter \"{param.Name}\" expects a boolean value (true/false), but received: \"{inputParam}\"");
                            return false;
                        }
                        break;

                    default:
                        Logging.Warning($"Unknown parameter type for \"{param.Name}\"");
                        return false;
                }
            }

            return true;
        }

        public static void SwitchHandle(string command)
        {
            // Here will be the command handling logic
        }

        public static async void SearchHandle(string input, Command command, CancellationTokenSource cts)
        {
            string[] inputCommandStructure = input.Split(' ');
            string commandName = inputCommandStructure[0];
            if (commandName != command.Name)
            {
                Logging.Warning($"Command name mismatch. Expected: \"{command.Name}\", but received: \"{commandName}\"");
                return;
            }

            // All exceptions are handled in the validator with logging warnings
            if (!Validator(inputCommandStructure, command.Parameters))
            {
                return;
            }

            string processMethodName = char.ToUpper(commandName[0]) + commandName.Substring(1);
            Type type = typeof(CommandHandler);
            MethodInfo? processMethod = type.GetMethod(processMethodName, BindingFlags.NonPublic | BindingFlags.Static);
            if (processMethod == null)
            {
                Logging.Warning($"No processor found for command \"{commandName}\"", toHistory: false);
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
        private static void Cmlst()
        {
            Logging.Message("* Available inline commands:");
            if (CommandManager.GetAllCommands().Count == 0)
            {
                Logging.Message("No commands have been loaded...");
                return;
            }

            foreach (Command cm in CommandManager.GetAllCommands())
            {
                string parametersNamesPerformance = cm.Parameters != null ? Colorist.ColoredFilterRGB(ParameterNameRGB[0], ParameterNameRGB[1], ParameterNameRGB[2]) + string.Join(" ", cm.Parameters.Select(p => $"\"{p.Name}\"")) + Colorist.ResetColor() : "";
                string descriptionPerformance = cm.Description ?? "Description is empty...";
                Logging.Message($"{Colorist.ColoredFilterRGB(CommandNameRGB[0], CommandNameRGB[1], CommandNameRGB[2])}- {cm.Name}{Colorist.ResetColor()} {parametersNamesPerformance} - {descriptionPerformance}");
            }
        }

        private static void Conlst()
        {
            ServerClientSession[] connections = SessionManager.Connections.Values.ToArray();
            if (connections.Length == 0)
            {
                Logging.Message("No active connections...");
                return;
            }

            int maxAddr = 0;
            int maxPort = 0;
            int maxRecv = 0;
            int maxSent = 0;
            foreach (ServerClientSession c in connections)
            {
                maxAddr = Math.Max(maxAddr, c.EndPoint?.Address.ToString().Length ?? 0);
                maxPort = Math.Max(maxPort, c.EndPoint?.Port.ToString().Length ?? 0);
                maxRecv = Math.Max(maxRecv, c.TotalPacketsReceived.ToString().Length);
                maxSent = Math.Max(maxSent, c.TotalPacketsSent.ToString().Length);
            }
            int maxCount = connections.Length.ToString().Length;

            Logging.Message("* Active connections list:");
            for (int i = 0; i < connections.Length; i++)
            {
                ServerClientSession c = connections[i];

                string index = (i + 1).ToString().PadLeft(maxCount);
                string ipAddress = c.EndPoint?.Address.ToString().PadLeft(maxAddr) ?? string.Empty;
                string port = c.EndPoint?.Port.ToString().PadRight(maxPort) ?? string.Empty;
                string receivedPackets = c.TotalPacketsReceived.ToString().PadRight(maxRecv);
                string sentPackets = c.TotalPacketsSent.ToString().PadRight(maxSent);

                Logging.Message($"{index}. {ipAddress}:{port} | Recv: {receivedPackets} | Sent: {sentPackets} | Last act: {c.LastTransferTime.ToLocalTime():HH:mm:ss}");
            }
        }

        private static void Banlst()
        {
            string[] bannedIPs = Firewall.GetBannedIPs();
            if (bannedIPs.Length == 0)
            {
                Logging.Message("No banned IPs...");
                return;
            }

            Logging.Message("* Banned IPs list:");
            int maxCounterLength = bannedIPs.Length.ToString().Length;
            int counter = 1;
            foreach (string ip in bannedIPs)
            {
                Logging.Message($"{String.Format($"{{0,{maxCounterLength}}}", counter.ToString())}. {ip}");
                counter++;
            }
        }

        private static void Clear()
        {
            Logging.ClearConsole();
        }

        private static void Shutdown(string input, CancellationTokenSource cts)
        {
            Logging.Output($"The \"{input}\" command received. Initiating shutdown process...");
            cts.Cancel();
        }

        private static void Screen(string input)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (SessionManager.Connections.TryGetValue(targetEndPoint, out ServerClientSession? session) && session != null)
            {
                ScreenshotRequest screenshotRequest = new()
                {
                    FormatID = (byte)ConfigurationManager.ScreenshotFrameFormat,
                    QualityPercent = (byte)ConfigurationManager.ScreenshotQualityPercentage
                };
                session.SendPacket(screenshotRequest);
                Logging.Message($"Successfully sent to {targetEndPoint}, waiting for remote screenshot...");
            }
            else
            {
                Logging.Message($"No connection found named \"{targetEndPoint}\"");
            }
        }

        private static async Task Stream(string input)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (SessionManager.Connections.TryGetValue(targetEndPoint, out ServerClientSession? session) && session != null)
            {
                StreamingRequest streamingRequest = new()
                {
                    IsActive = true,
                    FormatID = (byte)ConfigurationManager.StreamingFrameFormat,
                    Quality = (byte)ConfigurationManager.StreamingQualityPercentage,
                    FrameUpdateRate = ConfigurationManager.StreamingFrameUpdateRate,
                    TargetFPS = (short)ConfigurationManager.StreamingTargetFPS
                };
                session.SendPacket(streamingRequest);
                Logging.Message($"* Successfully sent to {targetEndPoint}, waiting for starting stream...");
                
                await WindowManager.ShowWindow();
                WindowManager.SetWindowTitle(ConfigurationManager.WindowTitle + " | " + targetEndPoint);
            }
            else
            {
                Logging.Message($"No connection found named \"{targetEndPoint}\"");
            }
        }

        private static async Task Dstream()
        {
            try
            {   
                IPEndPoint? ipEndPoint = WindowManager.StreamingClientEndPoint;
                if (ipEndPoint == null)
                {
                    Logging.Message("No active stream to stop...");
                    return;
                }
                
                string endPoint = ipEndPoint.ToString();
                if (SessionManager.Connections.TryGetValue(endPoint, out ServerClientSession? session) && session != null)
                {
                    StreamingRequest streamingRequest = new()
                    {
                        IsActive = false
                    };
                    session.SendPacket(streamingRequest);
                    Logging.Message($"* Successfully sent to {endPoint}, waiting for stopping stream...");
                }
                else
                {
                    Logging.Message($"No connection found named \"{endPoint}\"");
                }
            }
            finally
            {
                WindowManager.SetWindowTitle(ConfigurationManager.WindowTitle ?? "");
                await WindowManager.HideWindow();
            }
        }
    }
}
