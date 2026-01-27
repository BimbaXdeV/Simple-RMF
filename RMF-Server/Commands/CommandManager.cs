using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Commands
{
    internal class CommandManager
    {   
        private static readonly string CommandsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "commands.xml");
        private static readonly List<Command> Commands = [];

        public static void Load()
        {
            if (!File.Exists(CommandsPath))
            {
                Logging.Error($"Unable to load configuration on path: {CommandsPath}");
                return;
            }

            XDocument commandsDoc = XDocument.Load(CommandsPath);
            var commandsDict = commandsDoc.Element("Commands")?.Elements("add");

            if (commandsDict == null)
            {
                Logging.Error($"The commands file has been corrupted. Please check its integrity");
                return;
            }

            int initializedCommandsCounter = 0;
            foreach (var el in commandsDict)
            {
                string? cmName = el.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(cmName))
                {
                    Logging.Warning("Failed to load an empty command, missing");
                    continue;
                }

                // It doesn't matter whether a command has a description. The main thing is the name
                string cmDesc = el.Attribute("description")?.Value ?? "";

                string[] pNameIndexes = el.Attributes()
                    .Where(a => a.Name.LocalName.StartsWith("paramname"))
                    .Select(a => new string(a.Name.LocalName.Where(c => char.IsDigit(c)).ToArray()))
                    .ToArray();

                string[] pTypeIndexes = el.Attributes()
                    .Where(a => a.Name.LocalName.StartsWith("paramtype"))
                    .Select(a => new string(a.Name.LocalName.Where(c => char.IsDigit(c)).ToArray()))
                    .ToArray();

                if (!pNameIndexes.SequenceEqual(pTypeIndexes))
                {
                    Logging.Warning($"Failed to load command \"{cmName}\": parameter names and types mismatch");
                    continue;
                }

                List<CommandParameter> parameters = [];
                try
                {
                    foreach (string i in pNameIndexes)
                    {
                        // Parameter must be a not null string
                        XAttribute? paramNameAttr = el.Attribute($"paramname{i}");
                        if (paramNameAttr == null)
                        {
                            Logging.Warning($"Failed to load command \"{cmName}\": missing parameter name for \"paramname{i}\"");
                            continue;
                        }
                        parameters.Add(new CommandParameter
                        {
                            Name = paramNameAttr.Value,
                            Type = el.Attribute($"paramtype{i}")?.Value ?? "string"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logging.Warning($"Failed to load \"{cmName}\" command parameters from xml: {ex.Message}");
                    continue;
                }

                Command cm = new Command
                {
                    Name = cmName,
                    Description = cmDesc,
                    Parameters = parameters.ToArray()
                };
                Commands.Add(cm);
                initializedCommandsCounter++;
            }

            Logging.Output($"Commands successfully loaded: {initializedCommandsCounter} / {commandsDict.Count()} initialized");
        }

        public static Command? GetCommand(string name)
        {
            return Commands.FirstOrDefault(c => c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }

        public static Command? GetSimilarityCommand(string name)
        {
            return Commands.FirstOrDefault(c => c.Name != null && c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
