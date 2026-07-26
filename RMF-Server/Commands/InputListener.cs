using RMF.Core.Interfaces;
using RMF_Server.Configurations;
using RMF_Server.Debugger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class InputListener
    {
        private readonly ICommandHandler _commandHandler;
        private readonly ICommandManager _commandManager;
        private readonly ILoggingEngine? _logger;
        private readonly CommandConfig? _commandConfig;
        private readonly ListenerConfig? _listenerConfig;

        private readonly StringBuilder _inputBuffer;
        private readonly StringBuilder _suggestionBuffer;
        private bool _isListening;
        private readonly string _commandSign;

        public InputListener(
            ICommandHandler commandHandler,
            ICommandManager commandManager,
            ILoggingEngine? logger = null,
            CommandConfig? commandConfig = null,
            ListenerConfig? listenerConfig = null
        )
        {
            this._commandHandler = commandHandler;
            this._commandManager = commandManager;
            this._logger = logger;
            this._commandConfig = commandConfig;
            this._listenerConfig = listenerConfig;

            this._inputBuffer = new StringBuilder();
            this._suggestionBuffer = new StringBuilder();
            this._isListening = false;
            this._commandSign = "> " + this._commandConfig?.InlineCommandDefautSign ?? string.Empty;
        }

        public async Task StartListening(CancellationTokenSource cts)
        {
            if (this._isListening)
            {
                this._logger?.Warning("The input listener has already been launched previously, a duplicate cannot be started");
                return;
            }

            CancellationToken token = cts.Token;
            this._isListening = true;
            this._logger?.Output("Input listener successfully started waiting admin\'s command");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);

                        switch (key.Key)
                        {
                            case ConsoleKey.Enter:
                                if (this._inputBuffer.Length == 0)
                                {
                                    continue;
                                }

                                if (this._suggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + this._suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(this._suggestionBuffer.Length);
                                    this._suggestionBuffer.Clear();
                                }

                                string command = this._inputBuffer.ToString().Trim().ToLower();
                                this._inputBuffer.Clear();
                                Console.WriteLine();

                                string commandName = command.Split(' ')[0];
                                Command? cm = this._commandManager.GetCommand(commandName);
                                if (cm == null)
                                {
                                    this._logger?.Warning($"Unknown command: \"{commandName}\". Type \"{this._commandConfig?.InlineCommandDefautSign}cmlst\" to see all available inline commands.");
                                    this._logger?.SetAdminTyping(false);
                                    continue;
                                }

                                await _commandHandler.SearchHandle(command, cm, cts);
                                this._logger?.SetAdminTyping(false);
                                break;

                            case ConsoleKey.Escape:
                                if (this._suggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + this._suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(this._suggestionBuffer.Length);
                                    this._suggestionBuffer.Clear();
                                }

                                if (this._inputBuffer.Length > 0)
                                {
                                    this._inputBuffer.Clear();
                                    HideChars(Console.CursorLeft);
                                    this._logger?.SetAdminTyping(false);
                                }
                                break;

                            case ConsoleKey.Backspace:
                                if (this._suggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + this._suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(this._suggestionBuffer.Length);
                                    this._suggestionBuffer.Clear();
                                }

                                if (this._inputBuffer.Length > 0)
                                {
                                    RemovePreviousChar();
                                }

                                if (this._inputBuffer.Length == 0)
                                {
                                    HideChars(_commandSign.Length);
                                    this._logger?.SetAdminTyping(false);
                                }
                                break;

                            case ConsoleKey.Tab:
                                if (this._suggestionBuffer!.Length > 0)
                                {
                                    string suggestion = this._suggestionBuffer.ToString();
                                    // The cursor must return to its last position if it was previously moved to the left using the arrow keys
                                    Console.CursorLeft = _commandSign.Length + this._inputBuffer.Length;
                                    this._inputBuffer.Append(suggestion);
                                    Console.Write(suggestion);
                                    this._suggestionBuffer.Clear();
                                }
                                break;

                            case ConsoleKey.RightArrow:
                                if (this._inputBuffer.Length > 0 && Console.CursorLeft < _commandSign.Length + _inputBuffer!.Length)
                                {
                                    Console.CursorLeft++;
                                }
                                break;

                            case ConsoleKey.LeftArrow:
                                if (this._inputBuffer.Length > 0 && Console.CursorLeft > _commandSign.Length)
                                {
                                    Console.CursorLeft--;
                                }
                                break;

                            default:
                                // To avoid desync between buffer and console, all keys that do not correspond to characters are ignored
                                if (key.KeyChar == '\u0000')
                                {
                                    continue;
                                }

                                if (this._suggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + this._suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(this._suggestionBuffer.Length);
                                    this._suggestionBuffer.Clear();
                                }

                                // The "IsAdminTyping" flag blocks the logger from writing to the console until an administrator command is sent or cancelled
                                if (!this._logger?.GetAdminTyping() == true)
                                {
                                    Console.Write(this._commandSign);
                                    this._logger?.SetAdminTyping(true);
                                }
                                AddChar(key.KeyChar);

                                if (this._commandConfig != null &&
                                    this._commandConfig.InlineSuggestionsEnabled &&
                                    this._inputBuffer.Length >= this._commandConfig.InlineSuggestionsMinChars)
                                {
                                    string currentInput = this._inputBuffer.ToString();
                                    Command? predictedCommand = this._commandManager.GetSimilarityCommand(currentInput);
                                    if (predictedCommand != null && predictedCommand.Name!.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string suggestionPart = predictedCommand.Name.Substring(currentInput.Length);
                                        if (!string.IsNullOrEmpty(suggestionPart))
                                        {
                                            this._suggestionBuffer?.Append(suggestionPart);
                                            Console.Write($"{Colorist.ColoredFilterRGB(ThemeManager.AdminSuggestion[0], ThemeManager.AdminSuggestion[1], ThemeManager.AdminSuggestion[2])}{suggestionPart}{Colorist.ResetColor()}");
                                            Console.CursorLeft -= suggestionPart.Length;

                                        }
                                    }
                                }
                                break;
                        };
                    }

                    await Task.Delay(this._listenerConfig?.ListenerDelayMsecs ?? 100, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                this._inputBuffer?.Clear();
                this._suggestionBuffer?.Clear();
                this._isListening = false;
            }
        }

        private void AddChar(char c)
        {
            int currentLeftPos = Console.CursorLeft;
            int insertIndex = currentLeftPos - this._commandSign.Length;
            
            if (insertIndex == this._inputBuffer.Length)
            {
                this._inputBuffer.Append(c);
                Console.Write(c);
            }
            else
            {
                this._inputBuffer.Insert(insertIndex, c);
                string tail = this._inputBuffer.ToString().Substring(insertIndex);
                Console.Write(tail);
                Console.CursorLeft = currentLeftPos + 1;
            }
        }

        private void RemovePreviousChar()
        {
            int currentLeftPos = Console.CursorLeft;
            int removeIndex = currentLeftPos - this._commandSign.Length;
            
            if (removeIndex == this._inputBuffer.Length)
            {
                this._inputBuffer.Remove(this._inputBuffer.Length - 1, 1);
                Console.Write("\b \b");
            }
            else
            {
                this._inputBuffer.Remove(removeIndex, 1);
                string tail = this._inputBuffer.ToString().Substring(removeIndex);
                Console.Write("\b \b" + tail);
                Console.CursorLeft = currentLeftPos - 1;
            }
        }

        private void HideChars(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Console.Write("\b \b");
            }
        }
    }
}
