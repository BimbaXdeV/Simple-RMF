using RMF.Core.Interfaces;
using RMF.Core.Screen;
using RMF_Client.Logic;
using RMF_Client.Storage;
using Silk.NET.Maths;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal abstract class BaseCapturer : IScreenProvider
    {
        public short ScreenWidth;
        public short ScreenHeight;
        protected SKBitmap? ScreenBitmap;
        //protected ScreenPatch[]? ScreenPatches;
        protected IntPtr RawPixels => this.ScreenBitmap?.GetPixels() ?? IntPtr.Zero;
        protected readonly Lock CaptureProcessorLock = new();

        private ParallelOptions? Options;

        private int MetricsUpdateStep;
        private int FrameUpdateStep;

        public BaseCapturer()
        {
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
        //   (see MetricsUpdateRate in ConfigurationManager) to check for any changes in the screen metrics.
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


        // "AcquireUpdates()" is required to capture only the updated areas of the client screen.
        // -------------------------------------------------------------------------------------
        // - The method should not return any pixel data. The X structure stores only the metrics of the changed screen areas, which are then
        //   used by the engine to obtain raw bytes.
        // -------------------------------------------------------------------------------------
        // WARNING: Use the above array leases to store areas to avoid GC (Garbage Collector) load issues.
        // NOTICE: There is no need to return the array back, the engine already knows how to do this.
        protected abstract RectsMetadata? AcquireUpdates();

        private void PrepareParallelOptions()
        {
            // It is recommended to use half of all processor cores
            int maxCores = ConfigurationManager.MaxProcessorCores > 0 && ConfigurationManager.MaxProcessorCores <= Environment.ProcessorCount
                           ? ConfigurationManager.MaxProcessorCores : Environment.ProcessorCount / 2;

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

        public CapturedFrame? Capture(ScreenFormats format, byte quality, int frameUpdateRate = 0)
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0 || this.MetricsUpdateStep++ % ConfigurationManager.MetricsUpdateRate == 0)
            {
                UpdateBitmapMetrics();
                this.MetricsUpdateStep = 0;
            }

            bool isFullFrame = frameUpdateRate <= 0 || this.FrameUpdateStep++ % frameUpdateRate == 0;

            lock (this.CaptureProcessorLock)
            {
                if (isFullFrame)
                {
                    ScreenPatch frame = AcquireFrame();
                    if (frame.Data == null)
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

                // Partial frame with updates only
                else
                {
                    RectsMetadata? updatedPatches = AcquireUpdates();
                    try
                    {
                        if (!updatedPatches.HasValue || updatedPatches.Value.Count == 0)
                        {
                            return null;
                        }

                        ScreenPatch[] patches = ArrayPool<ScreenPatch>.Shared.Rent(updatedPatches.Value.Count);
                        short writtenCount = 0;
                        try
                        {
                            Parallel.For(0, updatedPatches.Value.Count, this.Options!, (int i) =>
                            {
                                Box2D<int> patch = updatedPatches.Value[i];

                                int rowLength = this.ScreenWidth * 4;
                                IntPtr srcPtr = this.RawPixels + (patch.Min.Y * rowLength) + (patch.Min.X * 4);

                                int patchWidth = patch.Max.X - patch.Min.X;
                                int patchHeight = patch.Max.Y - patch.Min.Y;
                                if (patchWidth <= 0 || patchHeight <= 0)
                                {
                                    return;
                                }

                                using SKImage image = SKImage.FromPixels(
                                    new SKImageInfo(patchWidth, patchHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                                    srcPtr,
                                    rowLength
                                );
                                using SKData? compressedData = ScreenEncoder.CompressImage(image, format, quality);
                                if (compressedData == null)
                                {
                                    return;
                                }

                                byte[] patchBuffer = ArrayPool<byte>.Shared.Rent((int)compressedData!.Size);
                                compressedData.AsSpan().CopyTo(patchBuffer);

                                patches[writtenCount++] = new ScreenPatch(
                                    patchBuffer,
                                    (int)compressedData.Size,
                                    (short)patch.Min.X,
                                    (short)patch.Min.Y,
                                    (short)patchWidth,
                                    (short)patchHeight
                                );
                            });
                        }
                        catch (Exception)
                        {
                            for (int i = 0; i < writtenCount; i++)
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
            }
        }
    }
}
