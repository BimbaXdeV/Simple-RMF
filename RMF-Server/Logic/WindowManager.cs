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

        //public static bool IsWindowOpen()
        //{
        //    return Window != null && Window.IsVisible;
        //}

        public static void ShowWindow()
        {
            Dispatcher.UIThread.InvokeAsync(() =>
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

        public static void HideWindow()
        {

            Dispatcher.UIThread.InvokeAsync(() =>
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
            ViewModel.UpdateFrame(frame, width, height);
        }
    }
}
