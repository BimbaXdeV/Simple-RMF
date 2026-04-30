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
        private const int MinCompressionQuality = 5;
        private const int MaxCompressionQuality = 100;

        public static SKData? CompressImage(SKImage image, ScreenFormats format, byte quality)
        {
            Console.WriteLine($"{image.Width} {image.Height} {format} {quality}");
            if (image.Width <= 0 || image.Height <= 0 || format == ScreenFormats.Raw)
            {
                Console.WriteLine("Invalid borders");
                return null;
            }

            int encodedQuality = (int)(quality > MinCompressionQuality && quality <= MaxCompressionQuality ? quality : 100);
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
