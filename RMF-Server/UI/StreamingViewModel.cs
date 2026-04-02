using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.UI
{
    public class StreamingViewModel : ReactiveObject
    {
        private WriteableBitmap? DisplaySourceObj;
        public WriteableBitmap? DisplaySource
        {
            get => DisplaySourceObj;
            set => this.RaiseAndSetIfChanged(ref DisplaySourceObj, value);
        }

        public void UpdateFrame(byte[] frame, int width, int height)
        {
            if (this.DisplaySource == null ||
                this.DisplaySource.PixelSize.Width != width ||
                this.DisplaySource.PixelSize.Height != height)
            {
                this.DisplaySource = new WriteableBitmap(
                    new Avalonia.PixelSize(width, height),
                    new Avalonia.Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul
                );
            }

            using MemoryStream ms = new(frame);
            try
            {
                this.DisplaySource = WriteableBitmap.Decode(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update frame in bitmap: {ex}");
            }
            this.RaisePropertyChanged(nameof(this.DisplaySource));
        }
    }
}
