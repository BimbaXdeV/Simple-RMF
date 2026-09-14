using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal sealed class HelpCommand : InlineCommand
    {
        private readonly ICommandViewer _commandViewer;
        private readonly IThemeManager _themeManager;

        private const byte CommandSyntaxMaxLength = 28;

        public override string Category => "Performance";
        public override string Name => "help";
        public override string Description => "Displays a list of available inline commands and their descriptions";
        public override string[]? Parameters => null;

        public HelpCommand(
            ICommandViewer commandViewer,
            IThemeManager themeManager,
            ILogger<CommandDispatcher> cmLogger,
            CommandConfig commandConfig
        ) : base(cmLogger, commandConfig)
        {
            _commandViewer = commandViewer;
            _themeManager = themeManager;
        }

        public override Task ExecuteAsync(string[] args, CancellationToken token)
        {
            CmLogger.LogInformation("Available inline commands:");
            if (_commandViewer.GetLoadedCommandsCount() <= 0)
            {
                CmLogger.LogInformation("No commands have been loaded...");
                return Task.CompletedTask;
            }

            IExecutableCommand[] commands = _commandViewer.GetLoadedCommands();
            byte categoryIndex = 1;

            ThemeColor categoryIndexColor = _themeManager.GetColor("CategoryIndex");
            ThemeColor commandColor = _themeManager.GetColor("CommandName");
            ThemeColor paramColor = _themeManager.GetColor("ParameterName");

            IEnumerable<IGrouping<string, IExecutableCommand>> groupedCommands = commands.GroupBy(c => c.Category);
            foreach (IGrouping<string, IExecutableCommand> group in groupedCommands)
            {
                string categoryName = !string.IsNullOrEmpty(group.Key)
                    ? group.Key
                    : "Uncategorized";

                CmLogger.LogInformation(
                    "{IndexColorStart}[{Index}]{IndexColorEnd} {CategoryName}:",
                    categoryIndexColor,
                    categoryIndex,
                    ThemeColor.AnsiReset,
                    char.ToUpper(group.Key[0]) + group.Key.Substring(1)
                );

                ushort commandIndex = 1;

                foreach (IExecutableCommand cm in group)
                {
                    string visibleParams = cm.Parameters?.Length > 0
                        ? " " + string.Join(" ", cm.Parameters.Select(p => '[' + p + ']'))
                        : string.Empty;
                    string visibleSyntax = $" {categoryIndex}.{commandIndex}. /{cm.Name}{visibleParams}";

                    string coloredParams = cm.Parameters?.Length > 0
                        ? $" {paramColor}{string.Join(" ", cm.Parameters.Select(p => '[' + p + ']'))}{ThemeColor.AnsiReset}"
                        : string.Empty;
                    string coloredSyntax = $" {categoryIndex}.{commandIndex}. {commandColor}/{cm.Name}{ThemeColor.AnsiReset}{coloredParams}";

                    int paddingLength = Math.Max(0, CommandSyntaxMaxLength - visibleSyntax.Length);
                    string padding = new(' ', paddingLength);

                    string description = cm.Description ?? "Description is empty...";

                    CmLogger.LogInformation(
                        "{CommandSyntax}{Padding} : {Description}",
                        coloredSyntax, padding, description
                    );
                    commandIndex++;
                }
                categoryIndex++;
            }
            return Task.CompletedTask;
        }
    }
}
