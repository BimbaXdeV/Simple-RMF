using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ReactiveUI;
using RMF.Core.Screen;
using RMF_Server.Debugger;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.UI
{
    public class StreamingViewModel : ReactiveObject
    {
        private WriteableBitmap? _displaySource;
        public WriteableBitmap? DisplaySource
        {
            get => _displaySource;
            set => this.RaiseAndSetIfChanged(ref _displaySource, value);
        }

        private bool _isOverlayEnabled;
        public bool IsOverlayEnabled
        {
            get => _isOverlayEnabled;
            set => this.RaiseAndSetIfChanged(ref _isOverlayEnabled, value);
        }

        private int _displayFps;
        public int DisplayFps
        {
            get => _displayFps;
            set => this.RaiseAndSetIfChanged(ref _displayFps, value);
        }

        private float _displayFrameTime;
        public float DisplayFrameTime
        {
            get => _displayFrameTime;
            set => this.RaiseAndSetIfChanged(ref _displayFrameTime, value);
        }

        public IPEndPoint? StreamingClientEndPoint;

        private DateTime HandleStartTime;
        private int HandledFramesCount;
        private int Fps;
        private float FrameTimeMsecs;

        private void ValidateSource(int width, int height)
        {
            if (this.DisplaySource == null ||
                this.DisplaySource.PixelSize.Width != width ||
                this.DisplaySource.PixelSize.Height != height)
            {
                this.DisplaySource = new WriteableBitmap(
                    new Avalonia.PixelSize(width, height),
                    new Avalonia.Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul
                );
            }
        }

        private DateTime UpdateStats(bool overlay = false)
        {
            DateTime updateTime = DateTime.Now;
            if ((updateTime - this.HandleStartTime).TotalSeconds >= 1.0f)
            {
                this.HandleStartTime = updateTime;
                this.Fps = this.HandledFramesCount;
                this.HandledFramesCount = 0;

                if (overlay)
                {
                    this.DisplayFps = this.Fps;
                    this.DisplayFrameTime = this.FrameTimeMsecs;
                }
            }
            return updateTime;
        }

        private void UpdateActuality(DateTime lastUpdatedTime)
        {
            this.HandledFramesCount++;
            this.FrameTimeMsecs = (float)(DateTime.Now - lastUpdatedTime).TotalMilliseconds;
        }

        public unsafe void UpdateFrame(ScreenPatch frame, bool updateOverlay = false)
        {

            ValidateSource(frame.Width, frame.Height);
            DateTime currentTime = UpdateStats(updateOverlay);

            using (ILockedFramebuffer buffer = this.DisplaySource!.Lock())
            {
                using MemoryStream ms = new(frame.Data, 0, frame.Length);
                using SKCodec codec = SKCodec.Create(ms);
                if (codec == null)
                {
                    Logging.Warning($"Failed to decode screen frame");
                    return;
                }

                SKImageInfo info = new(frame.Width, frame.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                if (info.RowBytes == buffer.RowBytes)
                {
                    codec.GetPixels(info, buffer.Address);
                }
                else
                {
                    byte[] decodedPixels = ArrayPool<byte>.Shared.Rent(info.BytesSize);
                    try
                    {
                        fixed (byte* decodedPtr = decodedPixels)
                        {
                            byte* displayPtr = (byte*)buffer.Address;
                            codec.GetPixels(info, (IntPtr)decodedPtr);

                            int frameRowLength = frame.Width * 4;
                            int screenRowLength = buffer.RowBytes;

                            if (frameRowLength == screenRowLength)
                            {
                                Unsafe.CopyBlock(displayPtr, decodedPtr, (uint)(frameRowLength * frame.Height));
                            }
                            else
                            {
                                byte* destPtr = displayPtr;
                                byte* srcPtr = decodedPtr;
                                for (int y = 0; y < frame.Height; y++)
                                {
                                    Unsafe.CopyBlock(destPtr, srcPtr, (uint)frameRowLength);
                                    srcPtr += frameRowLength;
                                    destPtr += screenRowLength;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error($"Failed to write a new frame into bitmap: {ex}");
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(decodedPixels);
                    }
                }

                UpdateActuality(currentTime);
                var displaySource = this.DisplaySource;
                this.DisplaySource = null;
                this.DisplaySource = displaySource;

                //Dispatcher.UIThread.Post(() =>
                //{
                //    this.RaisePropertyChanged(string.Empty);
                //}, DispatcherPriority.Render);

                //    byte[] decodedPixels = ArrayPool<byte>.Shared.Rent(info.BytesSize);
                //    try
                //    {
                //        fixed (byte* decodedPtr = decodedPixels)
                //        {
                //            codec.GetPixels(info, (IntPtr)decodedPtr);

                //            int frameRowLength = frame.Width * 4;
                //            if (frameRowLength == screenRowLength)
                //            {
                //                Unsafe.CopyBlock(displayPtr, decodedPtr, (uint)(frameRowLength * frame.Height));
                //            }
                //            else
                //            {
                //                byte* destPtr = displayPtr;
                //                byte* srcPtr = decodedPtr;
                //                for (int y = 0; y < frame.Height; y++)
                //                {
                //                    Unsafe.CopyBlock(destPtr, srcPtr, (uint)frameRowLength);
                //                    srcPtr += frameRowLength;
                //                    destPtr += screenRowLength;
                //                }
                //            }
                //            UpdateActuality(currentTime);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        Logging.Error($"Failed to write a new frame into bitmap: {ex}");
                //    }
                //    finally
                //    {
                //        ArrayPool<byte>.Shared.Return(decodedPixels);
                //    }
                //}
                //var displaySource = this.DisplaySource;
                //this.DisplaySource = null;
                //this.DisplaySource = displaySource;
            }
        }

        public unsafe void UpdatePatches(ReadOnlySpan<ScreenPatch> patches, int patchCount, bool updateOverlay = false)
        {
            DateTime currentTime = UpdateStats(updateOverlay);

            using (ILockedFramebuffer buffer = this.DisplaySource!.Lock())
            {
                int screenRowLength = buffer.RowBytes;
                byte* displayPtr = (byte*)buffer.Address;

                for (int i = 0; i < patchCount; i++)
                {
                    ScreenPatch patch = patches[i];
                    using MemoryStream ms = new(patch.Data, 0, patch.Length);
                    using SKCodec codec = SKCodec.Create(ms);
                    if (codec == null)
                    {
                        Logging.Warning($"Failed to decode screen patch");
                        continue;
                    }

                    SKImageInfo info = new(patch.Width, patch.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

                    byte[] decodedPixels = ArrayPool<byte>.Shared.Rent(info.BytesSize);
                    try
                    {
                        fixed (byte* decodedPtr = decodedPixels)
                        {
                            codec.GetPixels(info, (IntPtr)decodedPtr);

                            int patchRowLength = patch.Width * 4;
                            byte* destPtr = displayPtr + (patch.Y * patch.Width * 4) + (patch.X * 4);
                            byte* srcPtr = decodedPtr;

                            for (int y = 0; y < patch.Height; y++)
                            {
                                Unsafe.CopyBlock(destPtr, srcPtr, (uint)patchRowLength);
                                srcPtr += patchRowLength;
                                destPtr += screenRowLength;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error($"Failed to write a dirty rectangle into bitmap: {ex}");
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(decodedPixels);
                    }
                }
            }

            UpdateActuality(currentTime);
            var displaySource = this.DisplaySource;
            this.DisplaySource = null;
            this.DisplaySource = displaySource;
        }
    }
}
