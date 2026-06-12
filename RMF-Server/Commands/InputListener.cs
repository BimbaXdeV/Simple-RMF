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

                        if (key.Key == ConsoleKey.Enter)
                        {
                            if (InputBuffer!.Length == 0)
                            {
                                continue;
                            }

                            if (SuggestionBuffer!.Length > 0)
                            {
                                Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                RemoveChars(SuggestionBuffer.Length);
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
                        }

                        else if (key.Key == ConsoleKey.Backspace)
                        {
                            if (SuggestionBuffer!.Length > 0)
                            {
                                Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                RemoveChars(SuggestionBuffer.Length);
                                SuggestionBuffer.Clear();
                            }

                            if (InputBuffer!.Length > 0)
                            {
                                InputBuffer.Remove(InputBuffer.Length - 1, 1);
                                RemoveLastChar();
                            }

                            if (InputBuffer.Length == 0)
                            {
                                RemoveChars(CommandSign.Length);
                                Logging.IsAdminTyping = false;
                            }
                        }

                        else if (key.Key == ConsoleKey.Tab)
                        {
                            if (SuggestionBuffer!.Length > 0)
                            {
                                string suggestion = SuggestionBuffer.ToString();
                                InputBuffer!.Append(suggestion);
                                Console.Write(suggestion);
                                SuggestionBuffer.Clear();
                            }
                        }

                        //else if (key.Key == ConsoleKey.LeftArrow)
                        //{
                        //    if (Console.CursorLeft > CommandSign.Length)
                        //    {
                        //        Console.CursorLeft--;
                        //    }
                        //}

                        //else if (key.Key == ConsoleKey.RightArrow)
                        //{
                        //    if (Console.CursorLeft < InputBuffer!.Length || (SuggestionBuffer!.Length > 0 && Console.CursorLeft < InputBuffer.Length + SuggestionBuffer.Length))
                        //    {
                        //        Console.CursorLeft++;
                        //    }
                        //}

                        else
                        {
                            if (SuggestionBuffer!.Length > 0)
                            {
                                Console.SetCursorPosition(Console.CursorLeft + SuggestionBuffer.Length, Console.CursorTop);
                                RemoveChars(SuggestionBuffer.Length);
                                SuggestionBuffer.Clear();
                            }

                            if (!Logging.IsAdminTyping)
                            {
                                Console.Write(CommandSign);
                                Logging.IsAdminTyping = true;
                            }
                            InputBuffer?.Append(key.KeyChar);
                            Console.Write(key.KeyChar);

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
                                        Console.SetCursorPosition(Console.CursorLeft - suggestionPart.Length, Console.CursorTop);

                                    }
                                }
                            }
                        }
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
                IsListening = false;
            }
        }

        private static void RemoveLastChar()
        {
            Console.Write("\b \b");
        }

        private static void RemoveChars(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Console.Write("\b \b");
            }
        }
    }
}
