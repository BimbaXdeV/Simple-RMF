using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using ReactiveUI.Avalonia;
using RMF_Server.Debugger;
using RMF_Server.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class WindowManager
    {
        private static StreamingWindow? Window;
        private static StreamingViewModel ViewModel = new();

        public static readonly TaskCompletionSource UIInitSource = new();
        public static Task WaitForUIReady() => UIInitSource.Task;

        private static Thread? UIThread;
        private static readonly Lock ThreadSetupLock = new();

        private static void CreateWindow()
        {
            Window = new StreamingWindow();
            ViewModel = new StreamingViewModel();
            Window.DataContext = ViewModel;

            Window.Title = ConfigurationManager.WindowTitle;
            Window.Width = ConfigurationManager.WindowWidth;
            Window.Height = ConfigurationManager.WindowHeight;
            SetWindowTheme(ConfigurationManager.WindowTheme);

            Window.Closed += (s, e) => Window = null;
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
                             .LogToTrace();
        }

        public static void StartUI(CancellationToken token)
        {
            lock (ThreadSetupLock)
            {
                if (UIThread != null)
                {
                    return;
                }

                //app.SetupWithoutStarting();

                //ClassicDesktopStyleApplicationLifetime lifetime = new()
                //{
                //    ShutdownMode = ShutdownMode.OnExplicitShutdown
                //};
                //Application.Current!.ApplicationLifetime = lifetime;
                try
                {

                    Thread uiThread = new(() =>
                    {
                        AppBuilder app = BuildAvaloniaApp();

                        token.Register(() =>
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                                    lifetime.Shutdown();
                            });
                        });

                        app.StartWithClassicDesktopLifetime(args: [], shutdownMode: ShutdownMode.OnExplicitShutdown);
                    });

                    uiThread.TrySetApartmentState(ApartmentState.STA);

                    // Default name - "RMF-UI-Thread"
                    uiThread.Name = string.IsNullOrEmpty(ConfigurationManager.WindowTitle) ? "RMF-UI-Thread" : ConfigurationManager.WindowTitle;

                    // Default priority - 2 (Normal)
                    uiThread.Priority = ConfigurationManager.WindowPriority >= 0 && ConfigurationManager.WindowPriority <= 4 ? (ThreadPriority)ConfigurationManager.WindowPriority : (ThreadPriority)2;
                    uiThread.Start();
                }
                catch (Exception ex)
                {
                    Logging.Error($"UI Thread crashed with exception: {ex}");
                }
            }
        }

        public static void ShowWindow()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Window == null)
                {
                    CreateWindow();
                }

                Window!.Show();
            });
        }

        public static void HideWindow()
        {
            Dispatcher.UIThread.InvokeAsync(() => Window?.Hide());
        }

        public static void SetWindowTheme(string? theme)
        {
            if (Window != null)
            {
                ThemeVariant variant = theme switch
                {
                    "L" => ThemeVariant.Light,
                    "Light" => ThemeVariant.Light,
                    "D" => ThemeVariant.Dark,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
                Window.RequestedThemeVariant = variant;
            }
        }

        public static void UpdateFrame(byte[] frame, int width, int height)
        {
            ViewModel.UpdateFrame(frame, width, height);
        }
    }
}
