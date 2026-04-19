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
            if (image.Width <= 0 || image.Height <= 0 || format == ScreenFormats.Raw)
            {
                return null;
            }

            int encodedQuality = (int)(quality > MinCompressionQuality && quality <= MaxCompressionQuality ? quality : 100);
            SKEncodedImageFormat encodedFormat = format switch
            {
                ScreenFormats.Png => SKEncodedImageFormat.Png,
                ScreenFormats.WebP => SKEncodedImageFormat.Webp,
                _ => SKEncodedImageFormat.Jpeg,  // Will be set by default if someone tries to pass an unknown enum
            };

            return image.Encode(encodedFormat, quality);
        }

        //public static SKData[] CompressImages(IEnumerable<SKImage> images, ScreenFormats format, byte quality)
        //{
        //    SKData[] compressedImages = new SKData[images.Count()];

        //    int i = 0;
        //    foreach (SKImage image in images)
        //    {
        //        SKData? compressedImage = CompressImage(image, format, quality);
        //        if (compressedImage != null)
        //        {
        //            compressedImages[i] = compressedImage;
        //        }
        //        i++;
        //    }
        //    return compressedImages;
        //}
    }
}
