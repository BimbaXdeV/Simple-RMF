using Microsoft.Win32;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    [SupportedOSPlatform("windows")]
    internal partial class WindowsMonitor : BaseMonitor
    {
        // private AdapterDesc VideoAdapterDesc { get; set; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { this.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>(); }
        }
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private unsafe static AdapterDesc1? GetVideoAdapterDesc()
        {
            DXGI dxgi = DXGI.GetApi((INativeWindowSource)(INativeWindow)null!);
            ComPtr<IDXGIFactory1> factory = default;
            ComPtr<IDXGIAdapter1> adapter = default;

            try
            {
                Guid factoryGuid = IDXGIFactory1.Guid;
                if (dxgi.CreateDXGIFactory1(ref factoryGuid, (void**)&factory) == 0)
                {
                    if (factory.EnumAdapters1(0, ref adapter) == 0)
                    {
                        AdapterDesc1 desc;
                        adapter.Get().GetDesc1(&desc);
                        return desc;
                    }
                }
                return null;
            }
            finally
            {
                if (adapter.Handle != null)
                {
                    adapter.Dispose();
                }
                if (factory.Handle != null)
                {
                    factory.Dispose();
                }
                dxgi.Dispose();
            }
        }

        public override string CPUName()
        {
            return Registry.GetValue($@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null)?.ToString() ?? "Unknown";
        }

        public unsafe override string GPUName()
        {
            AdapterDesc1? adapterDesc = GetVideoAdapterDesc();
            if (adapterDesc != null && adapterDesc.HasValue)
            {
                AdapterDesc1 fixedDesc = adapterDesc.Value;
                string? videoProviderName = new(fixedDesc.Description);
                return !string.IsNullOrEmpty(videoProviderName) ? videoProviderName : "Unknown";
            }

            // A fallback method via the registry, which should ideally work if the factory or adapter cannot be created
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\WinSAT", "PrimaryAdapterString", null)?.ToString() ?? "Unknown";
        }

        public override double RAMCapacity()
        {
            MEMORYSTATUSEX memoryStatus = new()
            {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            if (GlobalMemoryStatusEx(ref memoryStatus))
            {
                return memoryStatus.ullTotalPhys;
            }
            return default;
        }

        public override double VRAMCapacity()
        {
            AdapterDesc1? desc = GetVideoAdapterDesc();
            if (desc != null && desc.HasValue)
            {
                return desc.Value.DedicatedVideoMemory;
            }
            return 0;
        }
    }
}
