using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Commands
{
    internal static class XmlCommandLoader
    {
        public static (List<Command> Data, int Total) Load(string path, ILoggingEngine? logger = null)
        {
            if (!File.Exists(path))
            {
                logger?.Error($"Unable to load commands on path: {path}");
                return ([], 0);
            }

            XDocument commandsDoc = XDocument.Load(path);
            var commandsDict = commandsDoc.Element("Commands")?.Elements("add");

            if (commandsDict == null)
            {
                logger?.Error($"The commands file has been corrupted. Please check its integrity");
                return ([], 0);
            }

            List<Command> commands = [];
            foreach (var el in commandsDict)
            {
                string? cmName = el.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(cmName))
                {
                    logger?.Warning("Failed to load an empty command, missing");
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
                    logger?.Warning($"Failed to load command \"{cmName}\": parameter names and types mismatch");
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
                            logger?.Warning($"Failed to load command \"{cmName}\": missing parameter name for \"paramname{i}\"");
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
                    logger?.Warning($"Failed to load \"{cmName}\" command parameters from xml: {ex.Message}");
                    continue;
                }

                Command cm = new()
                {
                    Name = cmName,
                    Description = cmDesc,
                    Parameters = parameters.ToArray()
                };
                commands.Add(cm);
            }

            return (commands, commandsDict.Count());
        }
    }
}
