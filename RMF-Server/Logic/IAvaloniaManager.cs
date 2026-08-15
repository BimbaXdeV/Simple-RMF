using Avalonia;
using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal interface IAvaloniaManager
    {
        IPEndPoint? StreamingClientEndPoint { get; set; }
        TaskCompletionSource UIInitSource { get; }
        Task WaitForUIReady();
        AppBuilder BuildAvaloniaApp();
        Task ShowWindow();
        Task HideWindow();
        void SetWindowTitle(string newTitle);
        void UpdateBitmap(ScreenPatch[] patches, int patchCount, bool isFullFrame);
    }
}
