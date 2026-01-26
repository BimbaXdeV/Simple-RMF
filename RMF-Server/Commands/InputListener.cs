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
        private static readonly byte[] AdminInputRGB = { 255, 255, 255 };
        private static readonly byte[] AdminSuggestionRGB = { 120, 120, 120 };

        private static readonly StringBuilder? InputBuffer = new();
        private static bool IsListening = false;

        private static string CommandSign = "> ";

        public static async Task StartListen(CancellationTokenSource cts)
        {
            if (IsListening)
            {
                Logging.Warning("The input listener has already been launched previously, a duplicate cannot be started");
                return;
            }

            CancellationToken token = cts.Token;
            IsListening = true;
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

                            string command = InputBuffer.ToString();
                            InputBuffer.Clear();
                            Console.WriteLine();
                            Logging.IsAdminTyping = false;

                            // It will be a command handler logic here...
                        }

                        else if (key.Key == ConsoleKey.Backspace)
                        {
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

                        else
                        {
                            if (!Logging.IsAdminTyping)
                            {
                                Console.Write(CommandSign);
                                Logging.IsAdminTyping = true;
                            }
                            InputBuffer!.Append(key.KeyChar);
                            Console.Write(key.KeyChar);
                        }
                    }

                    await Task.Delay(ConfigurationManager.InputListenerDelayMsecs, token);
                }
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
