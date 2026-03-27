using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.UI
{
    public class StreamingViewModel : ReactiveObject
    {
        public WriteableBitmap? DisplaySourceObj;
        public WriteableBitmap? DisplaySource
        {
            get => DisplaySourceObj;
            set => this.RaiseAndSetIfChanged(ref DisplaySourceObj, value);
        }

        public void UpdateFrame(byte[] frame, int width, int height)
        {
            if (this.DisplaySourceObj == null ||
                this.DisplaySourceObj.PixelSize.Width != width ||
                this.DisplaySourceObj.PixelSize.Height != height)
            {
                this.DisplaySourceObj = new WriteableBitmap(
                    new Avalonia.PixelSize(width, height),
                    new Avalonia.Vector(96, 96),
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Premul
                );
            }

            using MemoryStream ms = new(frame);
            using var bitmap = new Bitmap(ms);

        }
    }
}
