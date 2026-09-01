using RMF.Core.Screen;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Logic
{
    internal static class ScreenEncoder
    {
        private const int _minCompressionQuality = 5;
        private const int _maxCompressionQuality = 100;

        public static SKData? CompressImage(SKImage image, ScreenFormats format, byte quality)
        {
            if (image.Width <= 0 || image.Height <= 0 || format == ScreenFormats.Raw)
            {
                return null;
            }

            int encodedQuality = (int)(quality > _minCompressionQuality && quality <= _maxCompressionQuality ? quality : 100);
            SKEncodedImageFormat encodedFormat = format switch
            {
                ScreenFormats.Png => SKEncodedImageFormat.Png,
                ScreenFormats.WebP => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg,  // Will be set by default if someone tries to pass an unknown enum
            };

            return image.Encode(encodedFormat, encodedQuality);
        }
    }
}
