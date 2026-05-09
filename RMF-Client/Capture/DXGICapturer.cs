using RMF.Core.Screen;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal class DXGICapturer : BaseCapturer
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private ComPtr<ID3D11Device> Device;
        private ComPtr<ID3D11DeviceContext> Context;
        private ComPtr<IDXGIOutputDuplication> Duplication;
        private ComPtr<ID3D11Texture2D> Texture;
        
        private uint AcquireTimeoutCode = 0x887A0027;

        protected override unsafe void Initialize()
        {
            this.Device.Dispose();
            this.Context.Dispose();
            this.Duplication.Dispose();
            this.Texture.Dispose();

            var d3d11 = D3D11.GetApi((INativeWindowSource)(INativeWindow)null!);
            var dxgi = DXGI.GetApi((INativeWindowSource)(INativeWindow)null!);

            ID3D11Device* devicePtr;
            ID3D11DeviceContext* contextPtr;
            D3DFeatureLevel featureLevel;

            d3d11.CreateDevice(
                null,
                D3DDriverType.Hardware,
                0,
                (uint)CreateDeviceFlag.BgraSupport,
                null,
                0,
                D3D11.SdkVersion,
                &devicePtr,
                &featureLevel,
                &contextPtr
            );

            this.Device = new(devicePtr);
            this.Context = new(contextPtr);

            ComPtr<IDXGIDevice> dxgiDevice = default;
            this.Device.QueryInterface(out dxgiDevice);

            ComPtr<IDXGIAdapter> adapter = default;
            dxgiDevice.GetAdapter(adapter.GetAddressOf());

            ComPtr<IDXGIOutput> output = default;
            adapter.EnumOutputs(0, output.GetAddressOf());

            ComPtr<IDXGIOutput1> output1 = default;
            output.QueryInterface(out output1);

            ComPtr<IDXGIOutputDuplication> duplication = default;
            output1.DuplicateOutput(
                (IUnknown*)Device.Handle,
                duplication.GetAddressOf()
            );

            this.Duplication = duplication;

            Texture2DDesc textureDesc = new()
            {
                Width = (uint)this.ScreenWidth,
                Height = (uint)this.ScreenHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Staging,
                CPUAccessFlags = (uint)CpuAccessFlag.Read,
                BindFlags = 0,
                MiscFlags = 0
            };
            int hResult = this.Device.CreateTexture2D(ref textureDesc, null, this.Texture.GetAddressOf());
            if (hResult != 0)
            {
                throw new Exception("Failed to create 2D texture!");
            }
        }

        protected override void UpdateBitmapMetrics()
        {
            int ActualWidth = GetSystemMetrics(0);
            int ActualHeight = GetSystemMetrics(1);

            if (ActualWidth != this.ScreenWidth || ActualHeight != this.ScreenHeight)
            {
                lock (this.CaptureProcessorLock)
                {
                    this.ScreenWidth = (short)ActualWidth;
                    this.ScreenHeight = (short)ActualHeight;

                    this.ScreenBitmap?.Dispose();
                    this.ScreenBitmap = new SKBitmap(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                    Initialize();
                }
            }
        }

        private unsafe bool TryUnloadTexture(ComPtr<ID3D11Texture2D> texture)
        {
            if (texture.Handle == null || this.Texture.Handle == null)
            {
                return false;
            }

            this.Context.CopyResource((ID3D11Resource*)this.Texture.Handle, (ID3D11Resource*)texture.Handle);

            MappedSubresource mapped = default;
            int hResult = this.Context.Map((ID3D11Resource*)this.Texture.Handle, 0, Map.Read, 0, &mapped);
            if (hResult != 0 || mapped.PData == null)
            {
                return false;
            }

            try
            {
                byte* srcPtr = (byte*)mapped.PData;
                byte* destPtr = (byte*)this.RawPixels;
                int rowLength = this.ScreenWidth * 4;

                // Complete match of the bitmap and the resulting frame sizes
                if (mapped.RowPitch == rowLength)
                {
                    Unsafe.CopyBlock(destPtr, srcPtr, (uint)(rowLength * ScreenHeight));
                }

                // A crutch that needed in cases where the video card suddenly decided to align the data by adding empty bytes
                else
                {
                    for (int y = 0; y < this.ScreenHeight; y++)
                    {
                        Unsafe.CopyBlock(destPtr, srcPtr, (uint)rowLength);
                        srcPtr += mapped.RowPitch;
                        destPtr += rowLength;
                    }
                }
                return true;
            }
            finally
            {
                this.Context.Unmap((ID3D11Resource*)this.Texture.Handle, 0);
            }
        }

        private unsafe bool TryAcquireNextFrame(out OutduplFrameInfo frameInfo)
        {
            frameInfo = default;
            ComPtr<IDXGIResource> resource = default;

            fixed (OutduplFrameInfo* frameInfoPtr = &frameInfo)
            {
                int hResult = this.Duplication.AcquireNextFrame(10, frameInfoPtr, resource.GetAddressOf());

                if (hResult != 0)
                {
                    if (hResult == (int)this.AcquireTimeoutCode)
                    {
                        Initialize();
                    }
                    this.Duplication.ReleaseFrame();
                    return false;
                }
            }

            ComPtr<ID3D11Texture2D> texture = default;
            resource.QueryInterface(out texture);
            return TryUnloadTexture(texture);
        }

        protected override unsafe ScreenPatch AcquireFrame()
        {
            if (!TryAcquireNextFrame(out OutduplFrameInfo frameInfo))
            {
                return new ScreenPatch();
            }

            try
            {
                int bufferSize = this.ScreenWidth * this.ScreenHeight * 4;
                byte[] frameBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                fixed (byte* destPtr = frameBuffer)
                {
                    Unsafe.CopyBlock(destPtr, (void*)this.RawPixels, (uint)bufferSize);
                }

                return new ScreenPatch(
                    frameBuffer,
                    bufferSize,
                    0,
                    0,
                    this.ScreenWidth,
                    this.ScreenHeight
                );
            }
            finally
            {
                this.Duplication.ReleaseFrame();
            }
        }

        protected override unsafe RectsMetadata? AcquireUpdates(byte[] destinationFrameBuffer, int frameSize)
        {
            if (!TryAcquireNextFrame(out OutduplFrameInfo frameInfo))
            {
                return null;
            }

            try
            {
                Marshal.Copy(this.RawPixels, destinationFrameBuffer, 0, frameSize);

                uint metadataBufferSize = frameInfo.TotalMetadataBufferSize;
                if (metadataBufferSize <= 0)
                {
                    return null;
                }

                byte[] metadataBuffer = ArrayPool<byte>.Shared.Rent((int)metadataBufferSize);
                uint requiredBufferSize = 0;
                bool isOwnershipTransfered = false;
                try
                {
                    fixed (byte* destPtr = metadataBuffer)
                    {
                        int hResult = this.Duplication.GetFrameDirtyRects(
                            metadataBufferSize,
                            (Box2D<int>*)destPtr,
                            &requiredBufferSize
                        );

                        if (hResult != 0)
                        {
                            return new RectsMetadata();
                        }
                    }

                    int rectCount = (int)(requiredBufferSize / sizeof(Box2D<int>));
                    isOwnershipTransfered = true;  // There`s nothing left to break here (it seems)
                    return new RectsMetadata(metadataBuffer, rectCount);
                }
                finally
                {
                    if (!isOwnershipTransfered)
                    {
                        ArrayPool<byte>.Shared.Return(metadataBuffer);
                    }
                }
            }
            finally
            {
                this.Duplication.ReleaseFrame();
            }
        }

        //protected override unsafe ScreenPatch GetActualFrame()
        //{
        //    UpdateBitmapFrame();

        //    int size = this.ScreenWidth * this.ScreenHeight * 4;
        //    byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
        //    fixed (byte* destPtr = buffer)
        //    {
        //        Unsafe.CopyBlock(destPtr, (void*)this.RawPixels, (uint)size);
        //    }
        //    return new ScreenPatch(
        //        buffer,
        //        size,
        //        0,
        //        0,
        //        this.ScreenWidth,
        //        this.ScreenHeight
        //    );
        //}

        //protected override unsafe RectsMetadata? GetFrameUpdates()
        //{
        //    OutduplFrameInfo frameInfo = default;
        //    ComPtr<IDXGIResource> resource = default;

        //    int hResult = this.Duplication.AcquireNextFrame(10, &frameInfo, resource.GetAddressOf());
        //    if (hResult != 0)
        //    {
        //        if (hResult == this.AcquireTimeoutCode)
        //        {
        //            Initialize();
        //        }
        //        return null;
        //    }

        //    uint metadataBufferSize = frameInfo.TotalMetadataBufferSize;
        //    if (metadataBufferSize == 0)
        //    {
        //        return null;
        //    }

        //    byte[] metadatabuffer = ArrayPool<byte>.Shared.Rent((int)metadataBufferSize);
        //    uint requiredBufferSize = 0;
        //    bool isMetadataTransfered = false;
        //    try
        //    {
        //        fixed (byte* destPtr = metadatabuffer)
        //        {
        //            hResult = this.Duplication.GetFrameDirtyRects(metadataBufferSize, (Box2D<int>*)destPtr, &requiredBufferSize);
        //            if (hResult != 0)
        //            {
        //                return null;
        //            }

        //            isMetadataTransfered = true;  // There`s nothing left to break here (it seems)
        //            int rectCount = (int)(requiredBufferSize / sizeof(Box2D<int>));
        //            return new RectsMetadata(metadatabuffer, rectCount);
        //        }
        //    }
        //    finally
        //    {
        //        resource.Dispose();
        //        this.Duplication.ReleaseFrame();

        //        if (!isMetadataTransfered)
        //        {
        //            ArrayPool<byte>.Shared.Return(metadatabuffer);
        //        }
        //    }
        //}
    }
}
