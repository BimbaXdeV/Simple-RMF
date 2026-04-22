using RMF.Core.Screen;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;
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
                Format = Format.FormatB8G8R8A8Unorm,
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
                    //this.ScreenBitmap = new SKBitmap();
                    //this.ScreenBitmap.InstallPixels(
                    //    new SKImageInfo(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul),
                    //    this.RawPixels,
                    //    this.ScreenWidth * 4
                    //);
                    Initialize();
                    Console.WriteLine($"Bitmap metrics updated! {this.ScreenWidth}x{this.ScreenHeight}");
                }
            }
        }

        protected override unsafe void UpdateBitmapFrame()
        {
            OutduplFrameInfo frameInfo = default;
            ComPtr<IDXGIResource> resource = default;

            int hResult = this.Duplication.AcquireNextFrame(10, &frameInfo, resource.GetAddressOf());


            if (hResult == (int)this.AcquireTimeoutCode)
            {
                return;
            }

            if (hResult != 0)
            {
                Initialize();
                return;
            }

            ComPtr<ID3D11Texture2D> texture = default;
            resource.QueryInterface(out texture);

            if (this.Texture.Handle != null && texture.Handle != null)
            {
                this.Context.CopyResource((ID3D11Resource*)this.Texture.Handle, (ID3D11Resource*)texture.Handle);

                MappedSubresource mapped = default;
                hResult = this.Context.Get().Map((ID3D11Resource*)this.Texture.Handle, 0, Map.Read, 0, &mapped);
                if (hResult == 0 && mapped.PData != null)
                {
                    byte* srcPtr = (byte*)mapped.PData;
                    byte* destPtr = (byte*)this.RawPixels;

                    int rowLength = this.ScreenWidth * 4;
                    if (mapped.RowPitch == rowLength)
                    {
                        Unsafe.CopyBlock(destPtr, srcPtr, (uint)(rowLength * ScreenHeight));
                    }
                    else
                    {
                        for (int y = 0; y < this.ScreenHeight; y++)
                        {
                            Unsafe.CopyBlock(destPtr, srcPtr, (uint)rowLength);
                            srcPtr += mapped.RowPitch;
                            destPtr += rowLength;
                        }
                    }
                    this.Context.Unmap((ID3D11Resource*)this.Texture.Handle, 0);
                }
            }

            resource.Dispose();
            texture.Dispose();
            this.Duplication.Get().ReleaseFrame();
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

        protected override unsafe RectsMetadata? GetFrameUpdates()
        {
            OutduplFrameInfo frameInfo = default;
            using ComPtr<IDXGIResource> resource = default;

            int hResult = this.Duplication.AcquireNextFrame(10, &frameInfo, resource.GetAddressOf());
            if (hResult != 0)
            {
                if (hResult == this.AcquireTimeoutCode)
                {
                    Initialize();
                }
                return null;
            }

            uint metadataBufferSize = frameInfo.TotalMetadataBufferSize;
            if (metadataBufferSize == 0)
            {
                return null;
            }

            byte[] metadatabuffer = ArrayPool<byte>.Shared.Rent((int)metadataBufferSize);
            uint requiredBufferSize = 0;

            fixed (byte* destPtr = metadatabuffer)
            {
                hResult = this.Duplication.GetFrameDirtyRects(metadataBufferSize, (Box2D<int>*)destPtr, &requiredBufferSize);
                if (hResult != 0)
                {
                    return null;
                }

                int rectCount = (int)(metadataBufferSize / sizeof(Box2D<int>));
                return new RectsMetadata(metadatabuffer, rectCount);
            }
        }

        //protected override unsafe SKBitmap? GetScreenBitmap()
        //{
        //    lock (this.ScreenGetterLock)
        //    {
        //        OutduplFrameInfo frameInfo = default;
        //        ComPtr<IDXGIResource> resource = default;

        //        int hResult = this.Duplication.AcquireNextFrame(10, &frameInfo, resource.GetAddressOf());

        //        if (hResult == (int)this.AcquireTimeoutCode)
        //        {
        //            return this.ScreenBitmap;
        //        }

        //        if (hResult != 0)
        //        {
        //            Initialize();
        //            return this.ScreenBitmap;
        //        }

        //        ComPtr<ID3D11Texture2D> texture = default;
        //        resource.QueryInterface(out texture);

        //        if (this.Texture.Handle != null && texture.Handle != null)
        //        {
        //            this.Context.CopyResource((ID3D11Resource*)this.Texture.Handle, (ID3D11Resource*)texture.Handle);

        //            MappedSubresource mapped = default;
        //            hResult = this.Context.Get().Map((ID3D11Resource*)this.Texture.Handle, 0, Map.Read, 0, &mapped);
        //            if (hResult == 0 && mapped.PData != null)
        //            {
        //                byte* srcPtr = (byte*)mapped.PData;
        //                byte* destPtr = (byte*)this.ScreenBitmap!.GetPixels();

        //                int rowLength = this.ScreenWidth * 4;

        //                for (int y = 0; y < this.ScreenHeight; y++)

        //                {
        //                    Unsafe.CopyBlock(destPtr, srcPtr, (uint)rowLength);
        //                    srcPtr += mapped.RowPitch;
        //                    destPtr += rowLength;
        //                }

        //                this.Context.Unmap((ID3D11Resource*)this.Texture.Handle, 0);
        //            }
        //        }

        //        resource.Dispose();
        //        texture.Dispose();
        //        this.Duplication.Get().ReleaseFrame();
        //        return this.ScreenBitmap;
        //    }
        //}
    }
}
