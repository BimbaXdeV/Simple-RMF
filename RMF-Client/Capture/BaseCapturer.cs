using RMF.Core.Appearance;
using RMF.Core.Interfaces;
using RMF.Core.Screen;
using RMF_Client.Configurations;
using RMF_Client.Logic;
using Silk.NET.Maths;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal abstract class BaseCapturer : IScreenProvider
    {
        private readonly CaptureConfig _captureConfig;

        public short ScreenWidth;
        public short ScreenHeight;
        protected SKBitmap? ScreenBitmap;
        //protected ScreenPatch[]? ScreenPatches;
        protected IntPtr RawPixels => this.ScreenBitmap?.GetPixels() ?? IntPtr.Zero;
        protected readonly Lock CaptureProcessorLock = new();

        private ParallelOptions? Options;

        private int _metricsUpdateStep;
        private int _frameUpdateStep;

        public BaseCapturer(CaptureConfig captureConfig)
        {
            this._captureConfig = captureConfig;

            PrepareParallelOptions();
            UpdateBitmapMetrics();
            PrepareBitmap();
        }

        // "Initialize()" is required for initial setup of all necessary screen capture components.
        // -------------------------------------------------------------------------------------
        // - It doesn`t rent any data from the pool; everything is stored in the fields and properties of the inheriting class.
        // - It should be called when the monitor resolution is suddenly updated (see UpdateBitmapMetrics()).
        // -------------------------------------------------------------------------------------
        // NOTICE: You can also use this method to initialize any third-party screen capture libraries, if you choose to do so in the future.
        protected abstract void Initialize();


        // "UpdateBitmapMetrics()" is required to update the screen width and height, as well as to prepare the bitmap for capturing the screen.
        // -------------------------------------------------------------------------------------
        // - The function must overwrite the standard fields of the abstract class "ScreenWidth" and "ScreenHeight" with the current metrics of
        //   the client screen.
        // - It be called when the monitor resolution is suddenly updated, as well as periodically after a certain number of frames
        //   (see MetricsUpdateRate in CaptureConfigh) to check for any changes in the screen metrics.
        protected abstract void UpdateBitmapMetrics();


        // "AcquireFrame()" is required to capture the entire client screen and return it as a single patch.
        // -------------------------------------------------------------------------------------
        // - Even though the returned data type is a separate piece of the screen (ScreenPatch), your task is to place a complete image there
        //   while maintaining the current screen metrics.
        // -------------------------------------------------------------------------------------
        // WARNING: For stable operation in a high-load environment, it is necessary to use "ArrayPool<byte>.Shared.Rent(x)" rental for the
        //          resulting byte array (screen).
        // NOTICE: There is no need to think about returning the screen to the pool, this is already provided by the capture engine.
        protected abstract ScreenPatch AcquireFrame();

        private void PrepareParallelOptions()
        {
            // It is recommended to use half of all processor cores
            int maxCores = this._captureConfig.MaxProcessorCores > 0 && this._captureConfig.MaxProcessorCores <= Environment.ProcessorCount
                ? this._captureConfig.MaxProcessorCores
                : Environment.ProcessorCount / 2;

            this.Options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxCores
            };
        }

        private void PrepareBitmap()
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0)
            {
                return;
            }

            this.ScreenBitmap?.Dispose();
            this.ScreenBitmap = new SKBitmap(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        }

        public CapturedFrame? Capture(ScreenFormats format, byte quality, int frameUpdateRate = 0)
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0 || this._metricsUpdateStep++ % this._captureConfig.MetricsUpdateRate == 0)
            {
                UpdateBitmapMetrics();
                this._metricsUpdateStep = 0;
            }

            // If the frame update rate is set to 0 or less, it means that every frame should be captured.
            // Otherwise, only every Nth frame will be captured based on the specified frame update rate
            bool isFullFrame = frameUpdateRate <= 0 || this._frameUpdateStep++ % frameUpdateRate == 0;

            lock (this.CaptureProcessorLock)
            {
                // For capturers capable of working with dirty rectangles
                if (!isFullFrame && this is IDirtyRectsCapturer dirtyRectsCapturer)
                {
                    RectsMetadata? updatedPatches = dirtyRectsCapturer.AcquireUpdates();
                    if (!updatedPatches.HasValue || updatedPatches.Value.Count == 0)
                    {
                        return null;
                    }

                    try
                    {
                        ScreenPatch[] patches = ArrayPool<ScreenPatch>.Shared.Rent(updatedPatches.Value.Count);
                        short writtenCount = 0;
                        try
                        {
                            int screenSize = this.ScreenWidth * this.ScreenHeight * 4;

                            Array.Clear(patches, 0, updatedPatches.Value.Count);
                            Parallel.For(0, updatedPatches.Value.Count, this.Options!, (int i) =>
                            {
                                Box2D<int> patch = updatedPatches.Value[i];

                                if (patch.Max.X > this.ScreenWidth || patch.Max.Y > this.ScreenHeight)
                                {
                                    return;
                                }

                                int patchWidth = patch.Max.X - patch.Min.X;
                                int patchHeight = patch.Max.Y - patch.Min.Y;

                                if (patchWidth <= 0 || patchHeight <= 0)
                                {
                                    return;
                                }

                                int rowLength = this.ScreenWidth * 4;
                                int patchOffset = (patch.Min.Y * rowLength) + (patch.Min.X * 4);
                                IntPtr patchPtr = this.RawPixels + patchOffset;

                                using SKImage image = SKImage.FromPixels(
                                    new SKImageInfo(patchWidth, patchHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                                    patchPtr,
                                    rowLength
                                );
                                using SKData? compressedData = ScreenEncoder.CompressImage(image, format, quality);
                                if (compressedData == null)
                                {
                                    return;
                                }

                                byte[] patchBuffer = ArrayPool<byte>.Shared.Rent((int)compressedData!.Size);
                                compressedData.AsSpan().CopyTo(patchBuffer);

                                patches[i] = new ScreenPatch(
                                    patchBuffer,
                                    (int)compressedData.Size,
                                    (short)patch.Min.X,
                                    (short)patch.Min.Y,
                                    (short)patchWidth,
                                    (short)patchHeight
                                );
                            });

                            for (int i = 0; i < updatedPatches.Value.Count; i++)
                            {
                                if (patches[i].Data != null)
                                {
                                    patches[writtenCount++] = patches[i];
                                }
                            }
                        }
                        catch (Exception)
                        {
                            writtenCount = 0;
                            for (int i = 0; i < updatedPatches.Value.Count; i++)
                            {
                                if (patches[i] is IReleasable releasable)
                                {
                                    releasable.Release();
                                }
                            }
                        }

                        return new CapturedFrame(
                            patches,
                            writtenCount,
                            format,
                            false
                        );
                    }
                    finally
                    {
                        if (updatedPatches is IReleasable releasable)
                        {
                            releasable.Release();
                        }
                    }
                }

                // If the frame is a full frame or the capturer does not support dirty rects, capture the entire screen
                else
                {
                    ScreenPatch frame = AcquireFrame();
                    if (frame.Data == null || frame.Length <= 0)
                    {
                        return null;
                    }

                    unsafe
                    {
                        try
                        {
                            fixed (byte* srcPtr = frame.Data)
                            {
                                using SKImage image = SKImage.FromPixels(
                                    new SKImageInfo(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                                    (IntPtr)srcPtr,
                                    this.ScreenWidth * 4
                                );

                                using SKData? compressedData = ScreenEncoder.CompressImage(image, format, quality);
                                if (compressedData == null)
                                {
                                    return null;
                                }

                                int compressedSize = (int)compressedData!.Size;
                                byte[] frameBuffer = ArrayPool<byte>.Shared.Rent(compressedSize);
                                compressedData.AsSpan().CopyTo(frameBuffer);

                                // Screen patch array rental is used only for full image transfer, so you must return this array back
                                ScreenPatch[] patches = ArrayPool<ScreenPatch>.Shared.Rent(1);
                                patches[0] = new ScreenPatch(frameBuffer, compressedSize, 0, 0, this.ScreenWidth, this.ScreenHeight);

                                return new CapturedFrame(
                                    patches,
                                    1,
                                    format,
                                    true
                                );
                            }
                        }
                        finally
                        {
                            if (frame is IReleasable releasable)
                            {
                                releasable.Release();
                            }
                        }
                    }
                }
            }
        }
    }
}
