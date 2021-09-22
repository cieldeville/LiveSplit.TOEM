using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM
{
    public class WinAPI
    {
        //
        // CONSTANTS
        //

        // Memory Protection Constants
        //
        // https://docs.microsoft.com/en-us/windows/win32/memory/memory-protection-constants
        public const uint PAGE_NOACCESS = 0x01u;
        public const uint PAGE_READONLY = 0x02u;
        public const uint PAGE_READWRITE = 0x04u;
        public const uint PAGE_WRITECOPY = 0x08u;
        public const uint PAGE_EXECUTE = 0x10u;
        public const uint PAGE_EXECUTE_READ = 0x20u;
        public const uint PAGE_EXECUTE_READWRITE = 0x40u;
        public const uint PAGE_EXECUTE_WRITECOPY = 0x80u;
        public const uint PAGE_GUARD = 0x100u;
        public const uint PAGE_NOCACHE = 0x200u;
        public const uint PAGE_WRITECOMBINE = 0x400u;

        // Page State Constants
        //
        // https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-memory_basic_information
        public const uint MEM_COMMIT = 0x1000u;
        public const uint MEM_RESERVE = 0x2000u;
        public const uint MEM_FREE = 0x10000u;

        //
        // STRUCTS
        //

        // https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/ns-sysinfoapi-system_info
        public struct SystemInfo
        {
            public uint OemId;
            public uint PageSize;
            public UIntPtr MinimumApplicationAddress;
            public UIntPtr MaximumApplicationAddress;
            public IntPtr ActiveProcessorMask;
            public uint NumberOfProcessors;
            public uint ProcessorType;
            public uint AllocationGranularity;
            public ushort ProcessorLevel;
            public ushort ProcessorRevision;
        }

        // https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-memory_basic_information
        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicInformation
        {
            public bool Executable
            {
                get
                {
                    return Protect == PAGE_EXECUTE || Protect == PAGE_EXECUTE_READ || Protect == PAGE_EXECUTE_READWRITE || Protect == PAGE_EXECUTE_WRITECOPY;
                }
            }

            public bool Readable
            {
                get
                {
                    return Protect == PAGE_READONLY || Protect == PAGE_EXECUTE_READ || Protect == PAGE_READWRITE || Protect == PAGE_EXECUTE_READWRITE;
                }
            }

            public bool Writable
            {
                get
                {
                    return Protect == PAGE_READWRITE || Protect == PAGE_EXECUTE_READWRITE;
                }
            }

            public UIntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }


        //
        // FUNCTIONS
        //
        
        [DllImport("kernel32.dll")]
        public static extern void GetNativeSystemInfo(out SystemInfo lpSystemInfo);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MemoryBasicInformation lpBuffer, UIntPtr dwLength);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesWritten);

    }
}
