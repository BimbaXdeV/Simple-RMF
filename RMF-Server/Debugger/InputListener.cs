using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF_Server.Commands;
using RMF_Server.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal class InputListener : BackgroundService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ICommandRouter _commandRouter;
        private readonly ICommandViewer _commandViewer;
        private readonly IThemeManager _themeManager;
        private readonly ILogger<InputListener> _logger;
        private readonly IConsoleSynchronizer _consoleSync;
        private readonly CommandConfig _commandConfig;
        private readonly ListenerConfig _listenerConfig;

        private readonly StringBuilder _inputBuffer;
        private readonly StringBuilder _suggestionBuffer;
        private bool _isListening;
        private readonly string _commandSign;

        public InputListener(
            IHostApplicationLifetime lifetime,
            ICommandRouter commandRouter,
            ICommandViewer commandViewer,
            IThemeManager themeManager,
            ILogger<InputListener> logger,
            IConsoleSynchronizer consoleSync,
            CommandConfig commandConfig,
            ListenerConfig listenerConfig
        )
        {
            _lifetime = lifetime;
            _commandRouter = commandRouter;
            _commandViewer = commandViewer;
            _themeManager = themeManager;
            _logger = logger;
            _consoleSync = consoleSync;
            _commandConfig = commandConfig;
            _listenerConfig = listenerConfig;

            _inputBuffer = new StringBuilder();
            _suggestionBuffer = new StringBuilder();
            _isListening = false;
            _commandSign = "> " + _commandConfig.InlineCommandDefautSign;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            if (_isListening)
            {
                _logger.LogError("The input listener has already been launched previously, a duplicate cannot be started");
                return;
            }

            _isListening = true;
            _logger.LogInformation("Input listener successfully started waiting admin\'s command");
            try
            {
                await Task.Yield();
                while (!token.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo key = Console.ReadKey(true);

                        switch (key.Key)
                        {
                            case ConsoleKey.Enter:
                                if (_inputBuffer.Length == 0)
                                {
                                    continue;
                                }

                                if (_suggestionBuffer.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + _suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(_suggestionBuffer.Length);
                                    _suggestionBuffer.Clear();
                                }

                                string command = _inputBuffer.ToString().Trim().ToLower();
                                _inputBuffer.Clear();
                                Console.WriteLine();

                                string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                string commandHeader = parts[0];
                                string[] commandArgs = parts.Length >= 1 ? parts[1..] : [];

                                await _commandRouter.RouteCommandAsync(commandHeader, commandArgs, token);
                                _consoleSync.IsAdminTyping = false;

                                //InlineCommand? cm = _commandManager.GetCommand(commandName);
                                //if (cm == null)
                                //{
                                //    _logger.LogError("Unknown command: \"{CommandName}\". Type \"{CommandSign}cmlst\" to see all available inline commands", commandName, _commandConfig.InlineCommandDefautSign);
                                //    _consoleSync.IsAdminTyping = false;
                                //    continue;
                                //}

                                //await _commandHandler.SearchHandle(command, cm, token);
                                //_consoleSync.IsAdminTyping = false;
                                break;

                            case ConsoleKey.Escape:
                                if (_suggestionBuffer.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + _suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(_suggestionBuffer.Length);
                                    _suggestionBuffer.Clear();
                                }

                                if (_inputBuffer.Length > 0)
                                {
                                    _inputBuffer.Clear();
                                    HideChars(Console.CursorLeft);
                                    _consoleSync.IsAdminTyping = false;
                                }
                                break;

                            case ConsoleKey.Backspace:
                                if (_suggestionBuffer.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + _suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(_suggestionBuffer.Length);
                                    _suggestionBuffer.Clear();
                                }

                                if (_inputBuffer.Length > 0)
                                {
                                    RemovePreviousChar();
                                }

                                if (_inputBuffer.Length == 0)
                                {
                                    HideChars(_commandSign.Length);
                                    _consoleSync.IsAdminTyping = false;
                                }
                                break;

                            case ConsoleKey.Tab:
                                if (_suggestionBuffer.Length > 0)
                                {
                                    string suggestion = _suggestionBuffer.ToString();
                                    // The cursor must return to its last position if it was previously moved to the left using the arrow keys
                                    Console.CursorLeft = _commandSign.Length + _inputBuffer.Length;
                                    _inputBuffer.Append(suggestion);
                                    Console.Write(suggestion);
                                    _suggestionBuffer.Clear();
                                }
                                break;

                            case ConsoleKey.RightArrow:
                                if (_inputBuffer.Length > 0 && Console.CursorLeft < _commandSign.Length + _inputBuffer.Length)
                                {
                                    Console.CursorLeft++;
                                }
                                break;

                            case ConsoleKey.LeftArrow:
                                if (_inputBuffer.Length > 0 && Console.CursorLeft > _commandSign.Length)
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

                                if (_suggestionBuffer.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + _suggestionBuffer.Length, Console.CursorTop);
                                    HideChars(_suggestionBuffer.Length);
                                    _suggestionBuffer.Clear();
                                }

                                // The "IsAdminTyping" flag blocks the logger from writing to the console until an administrator command is sent or cancelled
                                if (!_consoleSync.IsAdminTyping)
                                {
                                    Console.Write(_commandSign);
                                    _consoleSync.IsAdminTyping = true;
                                }
                                AddChar(key.KeyChar);

                                if (_commandConfig.InlineSuggestionsEnabled &&
                                    _inputBuffer.Length >= _commandConfig.InlineSuggestionsMinChars)
                                {
                                    string currentInput = _inputBuffer.ToString();
                                    IExecutableCommand? predictedCommand = _commandViewer.FindSimilarCommand(currentInput);
                                    if (predictedCommand != null)
                                    {
                                        string suggestionPart = predictedCommand.Name.Substring(currentInput.Length);
                                        if (!string.IsNullOrEmpty(suggestionPart))
                                        {
                                            _suggestionBuffer.Append(suggestionPart);
                                            ThemeColor suggestionColor = _themeManager.GetColor("AdminSuggestion");
                                            Console.Write($"{suggestionColor}{suggestionPart}{ThemeColor.AnsiReset}");
                                            Console.CursorLeft -= suggestionPart.Length;

                                        }
                                    }
                                }
                                break;
                        };
                    }

                    await Task.Delay(_listenerConfig.ListenerDelayMsecs, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _inputBuffer.Clear();
                _suggestionBuffer.Clear();
                _isListening = false;

                _logger.LogInformation("The input listener has stopped, the use of admin commands is restricted");
            }
        }

        private void AddChar(char c)
        {
            int currentLeftPos = Console.CursorLeft;
            int insertIndex = currentLeftPos - _commandSign.Length;

            if (insertIndex == _inputBuffer.Length)
            {
                _inputBuffer.Append(c);
                Console.Write(c);
            }
            else
            {
                _inputBuffer.Insert(insertIndex, c);
                string tail = _inputBuffer.ToString().Substring(insertIndex);
                Console.Write(tail);
                Console.CursorLeft = currentLeftPos + 1;
            }
        }

        private void RemovePreviousChar()
        {
            int currentLeftPos = Console.CursorLeft;
            int removeIndex = currentLeftPos - _commandSign.Length;

            if (removeIndex == _inputBuffer.Length)
            {
                _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
                Console.Write("\b \b");
            }
            else
            {
                _inputBuffer.Remove(removeIndex, 1);
                string tail = _inputBuffer.ToString().Substring(removeIndex);
                Console.Write("\b \b" + tail);
                Console.CursorLeft = currentLeftPos - 1;
            }
        }

        private static void HideChars(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Console.Write("\b \b");
            }
        }
    }
}
