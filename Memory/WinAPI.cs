using System;
using System.Runtime.InteropServices;

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

        // Module Enumeration Filter Constants
        //
        // https://docs.microsoft.com/en-us/windows/win32/api/psapi/nf-psapi-enumprocessmodulesex
        public const uint LIST_MODULES_32BIT = 0x01u;
        public const uint LIST_MODULES_64BIT = 0x02u;
        public const uint LIST_MODULES_ALL = 0x03u;
        public const uint LIST_MODULES_DEFAULT = 0x00u;

        // Maximum Path Length Limitation
        //
        // https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=cmd
        public const int MAX_PATH = 260;

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
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "K32EnumProcessModulesEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, [Out] UIntPtr[] lphModule, uint cb, out uint lpcbNeeded, uint dwFilter);
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "K32GetModuleBaseNameA")]
        public static extern uint GetModuleBaseName(IntPtr hProcess, UIntPtr hModule, [Out] char[] lpBaseName, uint nSize);
    }
}
