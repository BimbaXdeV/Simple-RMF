using RMF.Core.Interfaces;
using RMF.Core.Screen;
using RMF_Client.Logic;
using RMF_Client.Storage;
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
        protected int ScreenWidth;
        protected int ScreenHeight;
        protected SKBitmap? ScreenBitmap;
        protected ScreenPatch[]? ScreenPatches;
        protected IntPtr RawPixels;
        protected readonly Lock ScreenGetterLock = new();

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
        protected abstract ScreenPatch GetActualFrame();
        protected abstract Memory<ScreenPatch> GetFrameUpdates();

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
            this.ScreenBitmap = new SKBitmap();
            this.ScreenBitmap.InstallPixels(
                new SKImageInfo(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                this.RawPixels,
                this.ScreenWidth * 4
            );
        }

        private void PreparePatchBuffer()
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0)
            {
                return;
            }
            int maxPatches = (this.ScreenWidth * this.ScreenHeight);
            this.ScreenPatches = new ScreenPatch[ConfigurationManager.DesktopPatchesBufferSize];
        }

        public CapturedFrame? Capture(ScreenFormats format, byte quality, int frameUpdateRate = 0)
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0 || this.MetricsUpdateStep++ % ConfigurationManager.MetricsUpdateRate == 0)
            {
                UpdateBitmapMetrics();
                this.MetricsUpdateStep = 0;
            }

            bool isFullFrame = this.FrameUpdateStep++ % frameUpdateRate == 0;

            if (isFullFrame)
            {
                ScreenPatch? actualFrame = GetActualFrame();
                if (actualFrame == null)
                {
                    this.FrameUpdateStep = 0;
                    return null;
                }

                try
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
                        format,
                        isFullFrame
                    );
                }
                finally
                {
                    this.FrameUpdateStep = 0;
                    ArrayPool<byte>.Shared.Return(actualFrame.Value.Data);
                }
            }

            // Partial frame with updates only
            else
            {
                ReadOnlyMemory<ScreenPatch> updatedPatches = GetFrameUpdates();
                if (updatedPatches.Length == 0)
                {
                    return null;
                }

                try
                {
                    Parallel.For(0, updatedPatches.Length, this.Options!, (i) => {
                        ScreenPatch patch = updatedPatches.Span[i];
                        if (patch.Data.Length == 0)
                        {
                            return;
                        }

                        int rowLength = this.ScreenWidth * 4;
                        IntPtr srcPtr = this.RawPixels + (patch.Y * rowLength) + (patch.X * 4);

                        using SKImage image = SKImage.FromPixels(
                            new SKImageInfo(patch.Width, patch.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
                            srcPtr,
                            rowLength
                        );
                        using SKData? data = ScreenEncoder.CompressImage(image, format, quality);

                        byte[] buffer = ArrayPool<byte>.Shared.Rent((int)data!.Size);
                        data.AsSpan().CopyTo(buffer);

                        this.ScreenPatches![i] = new ScreenPatch(buffer, (int)data.Size, patch.X, patch.Y, patch.Width, patch.Height);
                    });

                    return new CapturedFrame(
                        this.ScreenPatches!,
                        format,
                        isFullFrame
                    );
                }
                finally
                {
                    foreach (ScreenPatch patch in updatedPatches.Span)
                    {
                        ArrayPool<byte>.Shared.Return(patch.Data);
                    }
                }
            }
        }
    }
}
