using RMF_Server.Debugger;
using RMF_Server.Logic;
using RMF_Server.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static void SearchHandle(string input, Command command, CancellationTokenSource cts)
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

            if (processMethod.GetParameters().Length == 0)
            {
                processMethod.Invoke(null, null);
            }
            else
            {
                processMethod.Invoke(null, [input, cts]);
            }
        }

        // All command processors
        private static void Cmlst()
        {
            Console.WriteLine("Available inline commands:");
            if (CommandManager.GetAllCommands().Count == 0)
            {
                Console.WriteLine("No commands have been loaded...");
                return;
            }

            foreach (Command cm in CommandManager.GetAllCommands())
            {
                string parametersNamesPerformance = cm.Parameters != null ? Colorist.ColoredFilterRGB(ParameterNameRGB[0], ParameterNameRGB[1], ParameterNameRGB[2]) + string.Join(" ", cm.Parameters.Select(p => $"\"{p.Name}\"")) + Colorist.ResetColor() : "";
                string descriptionPerformance = cm.Description ?? "Description is empty...";
                Console.WriteLine($"{Colorist.ColoredFilterRGB(CommandNameRGB[0], CommandNameRGB[1], CommandNameRGB[2])}- {cm.Name}{Colorist.ResetColor()} {parametersNamesPerformance} - {descriptionPerformance}");
            }
        }

        private static void Conlst()
        {
            ClientSession[] connections = SessionManager.Connections.Values.ToArray();
            if (connections.Length == 0)
            {
                Console.WriteLine("No active connections...");
                return;
            }

            Console.WriteLine("Active connections list:");
            int maxCounterLength = connections.Length.ToString().Length;
            int counter = 1;
            foreach (ClientSession c in connections)
            {
                Console.WriteLine($"{String.Format($"{{0,{maxCounterLength}}}", counter.ToString())}. {c.EndPoint} | Last packet: {c.LastTransferTime}");
                counter++;
            }
        }

        private static void Banlst()
        {
            string[] bannedIPs = Firewall.GetBannedIPs();
            if (bannedIPs.Length == 0)
            {
                Console.WriteLine("No banned IPs...");
                return;
            }

            Console.WriteLine("Banned IPs list:");
            int maxCounterLength = bannedIPs.Length.ToString().Length;
            int counter = 1;
            foreach (string ip in bannedIPs)
            {
                Console.WriteLine($"{String.Format($"{{0,{maxCounterLength}}}", counter.ToString())}. {ip}");
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
    }
}
