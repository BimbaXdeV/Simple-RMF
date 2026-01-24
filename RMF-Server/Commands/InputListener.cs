using RMF_Server.Debugger;
using RMF_Server.Storage;
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
        private static readonly byte[] AdminAutoCorrectRGB = { 120, 120, 120 };

        private static readonly StringBuilder? InputBuffer = new();
        private static char LastChar = '0';
        private static bool IsListening = false;

        public static async Task StartListen(CancellationToken token)
        {
            if (IsListening)
            {
                Logging.Warning("The input listener has already been launched previously, a duplicate cannot be started");
                return;
            }

            IsListening = true;
            try
            {
                while (!token.IsCancellationRequested)
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
                    }

                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (InputBuffer!.Length > 0)
                        {
                            InputBuffer.Remove(InputBuffer.Length - 1, 1);
                            Console.Write("\b \b");
                        }

                        if (InputBuffer.Length == 0)
                        {
                            Logging.IsAdminTyping = false;
                        }
                    }

                    else
                    {
                        if (!Logging.IsAdminTyping)
                        {
                            Logging.IsAdminTyping = true;
                        }
                        InputBuffer!.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                    }

                    await Task.Delay(ConfigurationManager.InputListenerDelayMsecs);
                }
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                InputBuffer?.Clear();
                IsListening = false;
            }
        }
    }
}
