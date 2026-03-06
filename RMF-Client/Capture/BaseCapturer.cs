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
    internal abstract class BaseCapturer : IScreenCapturer
    {
        protected abstract SKBitmap GetScreenBitmap();

        public CapturedFrame? Capture(ScreenFormats format, byte quality)
        {
            using (SKBitmap bitmap = GetScreenBitmap())
            {
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

                return new CapturedFrame()
                {
                    Buffer = buffer,
                    Length = (int)data.Size
                };
            }
        }
    }
}
