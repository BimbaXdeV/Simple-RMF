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
        private WriteableBitmap? _displaySource;
        public WriteableBitmap? DisplaySource
        {
            get => _displaySource;
            set => this.RaiseAndSetIfChanged(ref _displaySource, value);
        }

        private bool _isOverlayEnabled;
        public bool IsOverlayEnabled
        {
            get => _isOverlayEnabled;
            set => this.RaiseAndSetIfChanged(ref _isOverlayEnabled, value);
        }

        private int _displayFps;
        public int DisplayFps
        {
            get => _displayFps;
            set => this.RaiseAndSetIfChanged(ref _displayFps, value);
        }

        private float _displayFrameTime;
        public float DisplayFrameTime
        {
            get => _displayFrameTime;
            set => this.RaiseAndSetIfChanged(ref _displayFrameTime, value);
        }

        private DateTime HandleStartTime;
        private int HandledFramesCount;
        private int Fps;
        private float FrameTimeMsecs;

        public void UpdateFrame(byte[] frame, int width, int height, bool updateOverlay = false)
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

            DateTime currentTime = DateTime.Now;
            if ((currentTime - this.HandleStartTime).TotalSeconds >= 1.0f)
            {
                this.HandleStartTime = currentTime;
                this.Fps = this.HandledFramesCount;
                this.HandledFramesCount = 0;

                if (updateOverlay)
                {
                    this.DisplayFps = this.Fps;
                    this.DisplayFrameTime = this.FrameTimeMsecs;
                }
            }

            using MemoryStream ms = new(frame);
            try
            {
                WriteableBitmap previous = this.DisplaySource;
                this.DisplaySource = WriteableBitmap.Decode(ms);
                previous.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update frame in bitmap: {ex}");
            }

            this.RaisePropertyChanged(nameof(this.DisplaySource));
            this.HandledFramesCount++;
            this.FrameTimeMsecs = (float)(DateTime.Now - currentTime).TotalMilliseconds;
        }
    }
}
