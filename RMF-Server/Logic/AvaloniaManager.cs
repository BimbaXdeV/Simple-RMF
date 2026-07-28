using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Threading;
using ReactiveUI.Avalonia;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Logic;
using RMF.Core.Interfaces.Network;
using RMF.Core.Packets;
using RMF.Core.Screen;
using RMF_Server.Configurations;
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
    internal class AvaloniaManager : IAvaloniaManager
    {
        private readonly ILoggingEngine _logger;
        private readonly AppearanceConfig _appearanceConfig;
        private readonly StreamingConfig _streamingConfig;

        private StreamingWindow? _window;
        private StreamingViewModel _viewModel;

        private int _isFrameProcessing;

        public readonly TaskCompletionSource UIInitSource;

        public IPEndPoint? StreamingClientEndPoint
        {
            get => this._viewModel.StreamingClientEndPoint;
            set => this._viewModel.StreamingClientEndPoint = value;
        }

        public AvaloniaManager(
            ILoggingEngine logger,
            AppearanceConfig appearanceConfig,
            StreamingConfig streamingConfig
        )
        {
            this._logger = logger;
            this._appearanceConfig = appearanceConfig;
            this._streamingConfig = streamingConfig;

            this._viewModel = new StreamingViewModel(this._logger);
            this._isFrameProcessing = 0;
            this.UIInitSource = new TaskCompletionSource();
        }

        public Task WaitForUIReady() => UIInitSource.Task;

        private void CreateWindow()
        {
            this._window = new StreamingWindow()
            {
                Title = this._appearanceConfig.WindowTitle
            };
            this._window.Title = this._appearanceConfig.WindowTitle;
            this._window.Width = this._appearanceConfig.WindowWidth;
            this._window.Height = this._appearanceConfig.WindowHeight;

            this._viewModel = new StreamingViewModel(this._logger)
            {
                IsOverlayEnabled = this._streamingConfig.EnableStreamingStatsOverlay
            };
            this._window.DataContext = this._viewModel;

            this._window.Closed += (s, e) => this._window = null;
        }

        public AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
                             .LogToTrace();
        }

        public async Task ShowWindow()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (this._window == null)
                {
                    CreateWindow();
                }

                if (!this._window!.IsVisible)
                {
                    this._window.Show();
                }
            });
        }

        public async Task HideWindow()
        {

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (this._window != null && this._window.IsVisible)
                {
                    this._window.Hide();
                }
            });
        }

        public void SetWindowTitle(string newTitle)
        {
            if (this._window == null)
            {
                this._logger?.Warning("Failed to update window title, the window instance is not initialized");
                return;
            }

            if (string.IsNullOrEmpty(newTitle))
            {
                this._logger?.Warning("Failed to update window title, received an empty string");
                return;
            }

            Dispatcher.UIThread.InvokeAsync(() => this._window.Title = newTitle);
        }

        public void SetWindowTheme(string? theme)
        {
            if (this._window != null)
            {
                ThemeVariant variant = theme switch
                {
                    "L" => ThemeVariant.Light,
                    "Light" => ThemeVariant.Light,
                    "D" => ThemeVariant.Dark,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
                this._window.RequestedThemeVariant = variant;
            }
        }

        private void ReturnRectsMemory(ScreenPatch[] patches, int patchCount)
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
                this._logger?.Warning($"Failed to return patch memory: {ex}");
            }
        }

        public void UpdateBitmap(ScreenPatch[] patches, int patchCount, bool isFullFrame)
        {
            if (patches == null || patches.Length == 0 || patchCount == 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref this._isFrameProcessing, 1, 0) == 1)
            {
                ReturnRectsMemory(patches, patchCount);
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    if (isFullFrame && patchCount == 1)
                    {
                        this._viewModel.UpdateFrame(patches[0], updateOverlay: this._streamingConfig.EnableStreamingStatsOverlay);
                    }
                    else
                    {
                        this._viewModel.UpdatePatches(patches, patchCount, updateOverlay: this._streamingConfig.EnableStreamingStatsOverlay);
                    }
                }
                catch (Exception ex)
                {
                    this._logger?.Warning($"Failed to update frame bitmap: {ex}");
                }
                finally
                {
                    Interlocked.Exchange(ref this._isFrameProcessing, 0);
                    ReturnRectsMemory(patches, patchCount);
                }
            }, priority: DispatcherPriority.Render);
        }
    }
}
