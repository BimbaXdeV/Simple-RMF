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
        protected ScreenPatch[] ScreenPatches = new ScreenPatch[100];
        protected IntPtr RawPixels;
        protected readonly Lock ScreenGetterLock = new();

        private int MetricsUpdateStep;
        private int FramesUpdateStep;

        public BaseCapturer()
        {
            UpdateBitmapMetrics();
            PrepareBitmap();
        }

        protected abstract void Initialize();
        protected abstract void UpdateBitmapMetrics();
        protected abstract void UpdateBitmapFrame();
        protected abstract ScreenPatch GetActualFrame();
        protected abstract Span<ScreenPatch> GetFrameUpdates();

        protected void PrepareBitmap()
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

        public CapturedFrame? Capture(ScreenFormats format, byte quality, int frameUpdateRate = 0)
        {
            if (this.ScreenWidth <= 0 || this.ScreenHeight <= 0 || this.MetricsUpdateStep++ % ConfigurationManager.MetricsUpdateRate == 0)
            {
                UpdateBitmapMetrics();
                this.MetricsUpdateStep = 0;
            }

            // One full frame
            if (this.FramesUpdateStep++ % frameUpdateRate == 0)
            {
                ScreenPatch? actualFrame = GetActualFrame();
                //this.ScreenBitmap.

                this.FramesUpdateStep = 0;
            }

            // Partial frame with updates only
            else
            {

            }

            SKBitmap? bitmap = GetFrameUpdates();
            if (bitmap == null)
            {
                return null;
            }

            SKEncodedImageFormat encodedFormat = format switch
            {
                ScreenFormats.WebP => SKEncodedImageFormat.Webp,
                ScreenFormats.Png => SKEncodedImageFormat.Png,
                _ => SKEncodedImageFormat.Jpeg,  // Will be set by default if someone tries to pass an unknown enum
            };

            int encodedQuality = (int)(quality > 0 && quality <= 100 ? quality : 100);

            using SKImage image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(encodedFormat, encodedQuality);

            byte[] buffer = ArrayPool<byte>.Shared.Rent((int)data.Size);
            data.AsSpan().CopyTo(buffer);

            return new CapturedFrame(buffer, (int)data.Size, this.ScreenWidth, this.ScreenHeight, format);
        }
    }
}
