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

            object? processResult = processMethod.GetParameters().Length == 0 ? processMethod.Invoke(null, null) : processMethod.Invoke(null, [input, cts]);
            if (processResult is Task taskResult)
            {
                await taskResult;
            }
        }

        // All command processors
        private static void Cmlst()
        {
            Logging.Message("Available inline commands:");
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

            Logging.Message("Active connections list:");
            int maxCounterLength = connections.Length.ToString().Length;
            int counter = 1;
            foreach (ServerClientSession c in connections)
            {
                Logging.Message($"{String.Format($"{{0,{maxCounterLength}}}", counter.ToString())}. {c.EndPoint} | Last packet: {c.LastTransferTime}");
                counter++;
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

            Logging.Message("Banned IPs list:");
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

        private static void Screen(string input, CancellationTokenSource cts)
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

        private static async Task Stream(string input, CancellationTokenSource cts)
        {
            string targetEndPoint = input.Split(' ')[1];
            if (SessionManager.Connections.TryGetValue(targetEndPoint, out ServerClientSession? session) && session != null)
            {
                StreamingRequest streamingRequest = new()
                {
                    IsActive = true,
                    FormatID = (byte)ConfigurationManager.StreamingFrameFormat,
                    Quality = (byte)ConfigurationManager.StreamingQualityPercentage,
                    IntervalMsecs = (short)ConfigurationManager.DesktopSendingIntervalMsecs
                };
                session.SendPacket(streamingRequest);
                Logging.Message($"Successfully sent to {targetEndPoint}, waiting for starting stream...");
                
                await WindowManager.ShowWindow();
                WindowManager.SetWindowTitle(ConfigurationManager.WindowTitle + " | " + targetEndPoint);
            }
            else
            {
                Logging.Message($"No connection found named \"{targetEndPoint}\"");
            }
        }

        private static async Task Dstream(string input, CancellationTokenSource cts)
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
                    Logging.Message($"Successfully sent to {endPoint}, waiting for stopping stream...");
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
