using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using ReactiveUI.Avalonia;
using RMF.Core.Packets;
using RMF.Core.Screen;
using RMF_Server.Debugger;
using RMF_Server.UI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        public static IPEndPoint? StreamingClientEndPoint
        {
            get => ViewModel.StreamingClientEndPoint;
            set => ViewModel.StreamingClientEndPoint = value;
        }

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

        private static void ReturnScreenMemory(ScreenPatch[] patches, int patchCount)
        {
            try
            {
                for (int i = 0; i < patchCount; i++)
                {
                    if (patches[i].Data != null)
                    {
                        ArrayPool<byte>.Shared.Return(patches[i].Data);
                    }
                }
                ArrayPool<ScreenPatch>.Shared.Return(patches);
            }
            catch (Exception ex)
            {
                Logging.Warning($"Failed to return screen memory: {ex}");
            }
        }

        public static void UpdateBitmap(ScreenPatch[] patches, int patchCount, bool isFullFrame)
        {
            if (patches == null || patches.Length == 0 || patchCount == 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref isFrameProcessing, 1, 0) == 1)
            {
                ReturnScreenMemory(patches, patchCount);
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (isFullFrame && patchCount == 1)
                    {
                        ViewModel.UpdateFrame(patches[0], updateOverlay: ConfigurationManager.EnableStreamingStatsOverlay);
                    }
                    else
                    {
                        ViewModel.UpdatePatches(patches, patchCount, updateOverlay: ConfigurationManager.EnableStreamingStatsOverlay);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Warning($"Failed to update frame bitmap: {ex}");
                }
                finally
                {
                    Interlocked.Exchange(ref isFrameProcessing, 0);
                    ReturnScreenMemory(patches, patchCount);
                }
            });
        }
    }
}
