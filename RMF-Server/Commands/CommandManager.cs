using RMF.Core.Interfaces;
using RMF_Server.Debugger;
using RMF_Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RMF_Server.Commands
{
    internal class CommandManager : ICommandManager
    {
        private readonly List<Command> _commands;

        public CommandManager(List<Command> commands)
        {
            this._commands = commands;
        }

        //public (int, int) Load()
        //{
        //    if (!File.Exists(_commandsPath))
        //    {
        //        this._logger?.Error($"Unable to load commands on path: {_commandsPath}");
        //        return (0, 0);
        //    }

        //    XDocument commandsDoc = XDocument.Load(_commandsPath);
        //    var commandsDict = commandsDoc.Element("Commands")?.Elements("add");

        //    if (commandsDict == null)
        //    {
        //        this._logger?.Error($"The commands file has been corrupted. Please check its integrity");
        //        return (0, 0);
        //    }

        //    int initializedCommandsCounter = 0;
        //    foreach (var el in commandsDict)
        //    {
        //        string? cmName = el.Attribute("name")?.Value;
        //        if (string.IsNullOrEmpty(cmName))
        //        {
        //            this._logger?.Warning("Failed to load an empty command, missing");
        //            continue;
        //        }

        //        // It doesn't matter whether a command has a description. The main thing is the name
        //        string cmDesc = el.Attribute("description")?.Value ?? "";

        //        string[] pNameIndexes = el.Attributes()
        //            .Where(a => a.Name.LocalName.StartsWith("paramname"))
        //            .Select(a => new string(a.Name.LocalName.Where(c => char.IsDigit(c)).ToArray()))
        //            .ToArray();

        //        string[] pTypeIndexes = el.Attributes()
        //            .Where(a => a.Name.LocalName.StartsWith("paramtype"))
        //            .Select(a => new string(a.Name.LocalName.Where(c => char.IsDigit(c)).ToArray()))
        //            .ToArray();

        //        if (!pNameIndexes.SequenceEqual(pTypeIndexes))
        //        {
        //            this._logger?.Warning($"Failed to load command \"{cmName}\": parameter names and types mismatch");
        //            continue;
        //        }

        //        List<CommandParameter> parameters = [];
        //        try
        //        {
        //            foreach (string i in pNameIndexes)
        //            {
        //                // Parameter must be a not null string
        //                XAttribute? paramNameAttr = el.Attribute($"paramname{i}");
        //                if (paramNameAttr == null)
        //                {
        //                    this._logger?.Warning($"Failed to load command \"{cmName}\": missing parameter name for \"paramname{i}\"");
        //                    continue;
        //                }
        //                parameters.Add(new CommandParameter
        //                {
        //                    Name = paramNameAttr.Value,
        //                    Type = el.Attribute($"paramtype{i}")?.Value ?? "string"
        //                });
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            this._logger?.Warning($"Failed to load \"{cmName}\" command parameters from xml: {ex.Message}");
        //            continue;
        //        }

        //        Command cm = new Command
        //        {
        //            Name = cmName,
        //            Description = cmDesc,
        //            Parameters = parameters.ToArray()
        //        };
        //        this._commands.Add(cm);
        //        initializedCommandsCounter++;
        //    }

        //    return (initializedCommandsCounter, commandsDict.Count());
        //}

        public Command? GetCommand(string name)
        {
            return this._commands.FirstOrDefault(c => c.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        }

        public List<Command> GetAllCommands()
        {
            return this._commands;
        }

        public Command? GetSimilarityCommand(string name)
        {
            return this._commands.FirstOrDefault(c => c.Name != null && c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
