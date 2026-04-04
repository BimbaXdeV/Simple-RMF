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
    internal static class WindowManager
    {
        private static StreamingWindow? Window;
        private static StreamingViewModel ViewModel = new();
        private static int isFrameProcessing = 0;

        public static readonly TaskCompletionSource UIInitSource = new();
        public static Task WaitForUIReady() => UIInitSource.Task;

        private static void CreateWindow()
        {
            Window = new StreamingWindow();
            Window.Title = ConfigurationManager.WindowTitle;
            Window.Width = ConfigurationManager.WindowWidth;
            Window.Height = ConfigurationManager.WindowHeight;
            SetWindowTheme(ConfigurationManager.WindowTheme);

            ViewModel = new StreamingViewModel();
            ViewModel.IsOverlayEnabled = ConfigurationManager.EnableStreamingStatsOverlay;
            Window.DataContext = ViewModel;

            Window.Closed += (s, e) => Window = null;
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
                             .LogToTrace();
        }

        public static async Task ShowWindow()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Window == null)
                {
                    CreateWindow();
                }

                if (!Window!.IsVisible)
                {
                    Window.Show();
                }
            });
        }

        public static async Task HideWindow()
        {

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Window != null && Window.IsVisible)
                {
                    Window.Hide();
                }
            });
        }

        public static void SetWindowTitle(string newTitle)
        {
            if (Window == null)
            {
                Logging.Warning("Failed to update window title, the window instance is not initialized");
                return;
            }

            if (string.IsNullOrEmpty(newTitle))
            {
                Logging.Warning("Failed to update window title, received an empty string");
                return;
            }

            if (newTitle.Length > AppearanceManager.MaxTitleLength)
            {
                Logging.Warning($"Failed to update window title, received too long string (max length: {AppearanceManager.MaxTitleLength})");
                return;
            }

            Dispatcher.UIThread.InvokeAsync(() => Window.Title = newTitle);
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
            if (Interlocked.CompareExchange(ref isFrameProcessing, 1, 0) == 1)
            {
                return;  // Skip this frame if another one is still being processed
            }

            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    ViewModel.UpdateFrame(frame, width, height, updateOverlay: ConfigurationManager.EnableStreamingStatsOverlay);
                });
            }
            finally
            {
                Interlocked.Exchange(ref isFrameProcessing, 0);
            }
        }
    }
}
