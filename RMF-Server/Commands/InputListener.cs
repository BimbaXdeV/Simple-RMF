using RMF_Server.Debugger;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Commands
{
    internal class InputListener
    {
        private static readonly StringBuilder? InputBuffer = new();
        private static readonly StringBuilder? SuggestionBuffer = new();
        private static bool IsListening = false;

        private static readonly string CommandSign = "> " + ConfigurationManager.InlineCommandDefautSign ?? "";

        public static async Task StartListen(CancellationTokenSource cts)
        {
            if (IsListening)
            {
                Logging.Warning("The input listener has already been launched previously, a duplicate cannot be started");
                return;
            }

            CancellationToken token = cts.Token;
            IsListening = true;
            Logging.Output("Input listener successfully started waiting admin\'s command");
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
                                if (InputBuffer!.Length == 0)
                                {
                                    continue;
                                }

                                if (SuggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                    HideChars(SuggestionBuffer.Length);
                                    SuggestionBuffer.Clear();
                                }

                                string command = InputBuffer.ToString().Trim().ToLower();
                                InputBuffer.Clear();
                                Console.WriteLine();

                                string commandName = command.Split(' ')[0];
                                Command? cm = CommandManager.GetCommand(commandName);
                                if (cm == null)
                                {
                                    Logging.Warning($"Unknown command: \"{commandName}\". Type \"{ConfigurationManager.InlineCommandDefautSign}cmlst\" to see all available inline commands.");
                                    Logging.IsAdminTyping = false;
                                    continue;
                                }

                                CommandHandler.SearchHandle(command, cm, cts);
                                Logging.IsAdminTyping = false;
                                break;

                            case ConsoleKey.Escape:
                                if (SuggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                    HideChars(SuggestionBuffer.Length);
                                    SuggestionBuffer.Clear();
                                }

                                if (InputBuffer!.Length > 0)
                                {
                                    InputBuffer.Clear();
                                    HideChars(Console.CursorLeft);
                                    Logging.IsAdminTyping = false;
                                }
                                break;

                            case ConsoleKey.Backspace:
                                if (SuggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                    HideChars(SuggestionBuffer.Length);
                                    SuggestionBuffer.Clear();
                                }

                                if (InputBuffer!.Length > 0)
                                {
                                    RemovePreviousChar();
                                }

                                if (InputBuffer.Length == 0)
                                {
                                    HideChars(CommandSign.Length);
                                    Logging.IsAdminTyping = false;
                                }
                                break;

                            case ConsoleKey.Tab:
                                if (SuggestionBuffer!.Length > 0)
                                {
                                    string suggestion = SuggestionBuffer.ToString();
                                    // The cursor must return to its last position if it was previously moved to the left using the arrow keys
                                    Console.CursorLeft = CommandSign.Length + InputBuffer!.Length;
                                    InputBuffer.Append(suggestion);
                                    Console.Write(suggestion);
                                    SuggestionBuffer.Clear();
                                }
                                break;

                            case ConsoleKey.RightArrow:
                                if (InputBuffer!.Length > 0 && Console.CursorLeft < CommandSign.Length + InputBuffer!.Length)
                                {
                                    Console.CursorLeft++;
                                }
                                break;

                            case ConsoleKey.LeftArrow:
                                if (InputBuffer!.Length > 0 && Console.CursorLeft > CommandSign.Length)
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

                                if (SuggestionBuffer!.Length > 0)
                                {
                                    Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                    HideChars(SuggestionBuffer.Length);
                                    SuggestionBuffer.Clear();
                                }

                                // The "IsAdminTyping" flag blocks the logger from writing to the console until an administrator command is sent or cancelled
                                if (!Logging.IsAdminTyping)
                                {
                                    Console.Write(CommandSign);
                                    Logging.IsAdminTyping = true;
                                }
                                AddChar(key.KeyChar);

                                if (ConfigurationManager.InlineSuggestionsEnabled && InputBuffer!.Length >= ConfigurationManager.InlineSuggestionsMinChars)
                                {
                                    string currentInput = InputBuffer.ToString();
                                    Command? predictedCommand = CommandManager.GetSimilarityCommand(currentInput);
                                    if (predictedCommand != null && predictedCommand.Name!.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string suggestionPart = predictedCommand.Name.Substring(currentInput.Length);
                                        if (!string.IsNullOrEmpty(suggestionPart))
                                        {
                                            SuggestionBuffer?.Append(suggestionPart);
                                            Console.Write($"{Colorist.ColoredFilterRGB(ThemeManager.AdminSuggestion[0], ThemeManager.AdminSuggestion[1], ThemeManager.AdminSuggestion[2])}{suggestionPart}{Colorist.ResetColor()}");
                                            Console.CursorLeft -= suggestionPart.Length;

                                        }
                                    }
                                }
                                break;
                        };
                    }

                    await Task.Delay(ConfigurationManager.InputListenerDelayMsecs, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                InputBuffer?.Clear();
                SuggestionBuffer?.Clear();
                IsListening = false;
            }
        }

        private static void AddChar(char c)
        {
            int currentLeftPos = Console.CursorLeft;
            int insertIndex = currentLeftPos - CommandSign.Length;
            
            if (insertIndex == InputBuffer!.Length)
            {
                InputBuffer!.Append(c);
                Console.Write(c);
            }
            else
            {
                InputBuffer.Insert(insertIndex, c);
                string tail = InputBuffer.ToString().Substring(insertIndex);
                Console.Write(tail);
                Console.CursorLeft = currentLeftPos + 1;
            }
        }

        private static void RemovePreviousChar()
        {
            int currentLeftPos = Console.CursorLeft;
            int removeIndex = currentLeftPos - CommandSign.Length;
            
            if (removeIndex == InputBuffer!.Length)
            {
                InputBuffer.Remove(InputBuffer.Length - 1, 1);
                Console.Write("\b \b");
            }
            else
            {
                InputBuffer.Remove(removeIndex, 1);
                string tail = InputBuffer.ToString().Substring(removeIndex);
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
