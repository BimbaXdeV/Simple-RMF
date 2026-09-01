using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Loaders;
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
        public static LoadResult<List<Command>> Load(string path)
        {
            if (!File.Exists(path))
            {
                return LoadResult<List<Command>>.Failure($"Unable to load commands on path: {path}");
            }

            try
            {
                XDocument commandsDoc = XDocument.Load(path);
                IEnumerable<XElement>? commandsDict = commandsDoc.Element("Commands")?.Elements("add");

                if (commandsDict == null)
                {
                    return LoadResult<List<Command>>.Failure($"The commands file has been corrupted. Please check its integrity");
                }

                List<Command> commands = [];
                foreach (XElement el in commandsDict)
                {
                    string? cmName = el.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(cmName))
                    {
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
                                continue;
                            }
                            parameters.Add(new CommandParameter
                            {
                                Name = paramNameAttr.Value,
                                Type = el.Attribute($"paramtype{i}")?.Value ?? "string"
                            });
                        }

                        Command cm = new()
                        {
                            Name = cmName,
                            Description = cmDesc,
                            Parameters = parameters.ToArray()
                        };
                        commands.Add(cm);
                    }
                    catch
                    {
                        // Simply skip it; the corrupted command will simply not be registered,
                        // and the server will continue operating without it :)
                    }
                }

                return LoadResult<List<Command>>.Success(commands, commands.Count, commandsDict.Count());
            }
            catch (Exception ex)
            {
                return LoadResult<List<Command>>.Failure(ex.Message);
            }
        }
    }
}
