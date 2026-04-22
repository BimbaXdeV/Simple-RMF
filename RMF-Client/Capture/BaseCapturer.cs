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
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal abstract class BaseCapturer : IScreenProvider
    {
        public short ScreenWidth;
        public short ScreenHeight;
        protected SKBitmap? ScreenBitmap;
        protected ScreenPatch[]? ScreenPatches;
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
            PreparePatchBuffer();
        }

        protected abstract void Initialize();
        protected abstract void UpdateBitmapMetrics();
        protected abstract void UpdateBitmapFrame();
        // protected abstract ScreenPatch GetActualFrame();
        protected abstract RectsMetadata? GetFrameUpdates();

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
            //this.ScreenBitmap = new SKBitmap();
            //this.ScreenBitmap.InstallPixels(
            //    new SKImageInfo(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
            //    this.RawPixels,
            //    this.ScreenWidth * 4
            //);
        }

        private void PreparePatchBuffer()
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0)
            {
                return;
            }
            this.ScreenPatches = new ScreenPatch[ConfigurationManager.DesktopPatchesBufferSize];
        }

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
                UpdateBitmapFrame();

                if (isFullFrame)
                {
                    using SKImage image = SKImage.FromPixels(
                        new SKImageInfo(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                        this.RawPixels,
                        this.ScreenWidth * 4
                    );
                    using SKData? compressedData = ScreenEncoder.CompressImage(image, format, quality);
                    int compressedSize = (int)compressedData!.Size;

                    byte[] buffer = ArrayPool<byte>.Shared.Rent(compressedSize);
                    compressedData.AsSpan().CopyTo(buffer);

                    // Screen patch array rental is used only for full image transfer, so you must return this array back
                    ScreenPatch[] patches = ArrayPool<ScreenPatch>.Shared.Rent(1);
                    patches[0] = new ScreenPatch(buffer, compressedSize, 0, 0, this.ScreenWidth, this.ScreenHeight);

                    return new CapturedFrame(
                        patches,
                        1,
                        format,
                        isFullFrame
                    );
                }

                // Partial frame with updates only
                else
                {
                    RectsMetadata? updatedPatches = GetFrameUpdates();
                    if (!updatedPatches.HasValue || updatedPatches.Value.Count == 0)
                    {
                        return null;
                    }
                    try
                    {
                        
                        Parallel.For(0, updatedPatches.Value.Count, this.Options!, (int i) =>
                        {
                            Box2D<int> patch = updatedPatches.Value[i];

                            int rowLength = this.ScreenWidth * 4;
                            IntPtr srcPtr = this.RawPixels + (patch.Min.Y * rowLength) + (patch.Min.X * 4);

                            using SKImage image = SKImage.FromPixels(
                                new SKImageInfo(patch.Max.X, patch.Max.Y, SKColorType.Bgra8888, SKAlphaType.Premul),
                                srcPtr,
                                rowLength
                            );
                            using SKData? data = ScreenEncoder.CompressImage(image, format, quality);
                            if (data == null)
                            {
                                Console.WriteLine("Failed to compress a patch!");
                                return;
                            }

                            byte[] patchBuffer = ArrayPool<byte>.Shared.Rent((int)data!.Size);
                            data.AsSpan().CopyTo(patchBuffer);

                            this.ScreenPatches![i] = new ScreenPatch(
                                patchBuffer,
                                (int)data.Size,
                                (short)patch.Min.X,
                                (short)patch.Min.Y,
                                (short)patch.Max.X,
                                (short)patch.Max.Y
                            );
                        });

                        return new CapturedFrame(
                            this.ScreenPatches!,
                            (short)updatedPatches.Value.Count,
                            format,
                            isFullFrame
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
