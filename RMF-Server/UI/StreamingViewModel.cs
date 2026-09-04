using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using RMF.Core.Interfaces;
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
        private readonly ILogger _logger;

        public StreamingViewModel(ILogger logger) : base()
        {
            this._logger = logger;
        }

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
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul
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

        private static string TranslateCodecExceptionMessage(SKCodecResult result)
        {
            // Yes, it looks like "Borsch borsch = new Borsh().GetBorsch()", but it isa rather important analysis tool,
            // albeit a crutch. To be honest, I haven`t yet had time to test which error is triggered by which situation,
            // so here are the raw lines :>
            return result switch
            {
                SKCodecResult.IncompleteInput => "incomplete input data",
                SKCodecResult.ErrorInInput => "error in input data",
                SKCodecResult.InvalidConversion => "invalid conversion",
                SKCodecResult.InvalidScale => "invalid scale",
                SKCodecResult.InvalidParameters => "invalid parameters",
                SKCodecResult.InvalidInput => "invalid input data",
                SKCodecResult.CouldNotRewind => "could not rewind input",
                SKCodecResult.InternalError => "internal error occurred",
                SKCodecResult.Unimplemented => "unimplemented codec method",
                _ => "unknown error occurred"
            };
        }

        public unsafe void UpdateFrame(ScreenPatch frame, bool updateOverlay = false)
        {
            ValidateSource(frame.Width, frame.Height);
            DateTime currentTime = UpdateStats(updateOverlay);

            using (ILockedFramebuffer buffer = this.DisplaySource!.Lock())
            {
                using MemoryStream ms = new(frame.Data, 0, frame.Length);
                using SKCodec codec = SKCodec.Create(ms, out SKCodecResult result);
                if (codec == null || result != SKCodecResult.Success)
                {
                    this._logger.LogError("Failed to decode screen frame: {Exception}", TranslateCodecExceptionMessage(result));
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
                        this._logger.LogError("Failed to write a new frame into bitmap: {Exception}", ex);
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
            }
        }

        public unsafe void UpdatePatches(ReadOnlySpan<ScreenPatch> patches, bool updateOverlay = false)
        {
            DateTime currentTime = UpdateStats(updateOverlay);

            using (ILockedFramebuffer buffer = this.DisplaySource!.Lock())
            {
                int screenRowLength = buffer.RowBytes;
                byte* displayPtr = (byte*)buffer.Address;

                //Console.WriteLine($"Patches processing ({patches.Length}):");
                for (int i = 0; i < patches.Length; i++)
                {
                    ScreenPatch patch = patches[i];
                    if (patch.Length <= 0 || patch.Data == null)
                    {
                        this._logger.LogWarning("Received an empty patch, nothing to do");
                        continue;
                    }

                    //Console.WriteLine(i + ". " + string.Join(' ', patches[..4].ToArray()) + ", L: " + patch.Length);
                    using MemoryStream ms = new(patch.Data, 0, patch.Length);
                    using SKCodec codec = SKCodec.Create(ms, out SKCodecResult result);
                    if (codec == null)
                    {
                        this._logger.LogWarning("Failed to decode screen patch: {Exception}", TranslateCodecExceptionMessage(result));
                        continue;
                    }

                    SKImageInfo info = new(patch.Width, patch.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

                    byte[] decodedPixels = ArrayPool<byte>.Shared.Rent(info.BytesSize);
                    try
                    {
                        fixed (byte* decodedPtr = decodedPixels)
                        {
                            codec.GetPixels(info, (IntPtr)decodedPtr);

                            byte* destPtr = displayPtr + (patch.Y * screenRowLength) + (patch.X * 4);
                            byte* srcPtr = decodedPtr;

                            int patchRowLength = patch.Width * 4;
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
                        this._logger.LogError("Failed to write a dirty rectangle into bitmap: {Exception}", ex);
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
