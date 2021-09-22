using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM.Memory
{
    /// <summary>
    /// A utility class that provides access to a foreign process' memory.
    /// </summary>
    public class MemoryInterface
    {
        /// <summary>
        /// The process whose memory the interface is accessing.
        /// </summary>
        public Process Process { get; }

        /// <summary>
        /// Constructs a new memory interface to access the specified process' memory.
        /// </summary>
        /// <param name="process">The process whose memory to access</param>
        public MemoryInterface(Process process)
        {
            Process = process;
        }

        /// <summary>
        /// Retrieves information about the native system in a SystemInfo structure.
        /// </summary>
        /// <returns>A WinAPI.SystemInfo structure containing information about the native system.</returns>
        public WinAPI.SystemInfo GetSystemInfo()
        {
            WinAPI.GetNativeSystemInfo(out WinAPI.SystemInfo systemInfo);
            return systemInfo;
        }

        /// <summary>
        /// Retrieves a list of MemoryBasicInformation structures representing all blocks of memory currently
        /// allocated by the interfaces process.
        /// </summary>
        /// <param name="filter">An optional filter for the returned pages ; returns true if a page should be added to the result set</param>
        /// <returns>A list of MemoryBasicInformation structures</returns>
        public List<WinAPI.MemoryBasicInformation> GetMemoryInfo(Func<WinAPI.MemoryBasicInformation, bool> filter = null)
        {
            List<WinAPI.MemoryBasicInformation> infos = new List<WinAPI.MemoryBasicInformation>();
            WinAPI.SystemInfo systemInfo = GetSystemInfo();

            UIntPtr cursor = systemInfo.MinimumApplicationAddress;
            do
            {
                WinAPI.MemoryBasicInformation info = default(WinAPI.MemoryBasicInformation);
                IntPtr ret = WinAPI.VirtualQueryEx(Process.Handle, cursor, out info, (UIntPtr)Marshal.SizeOf(info));
                if (ret == IntPtr.Zero)
                {
                    break;
                }
                
                if (filter == null || filter.Invoke(info))
                {
                    infos.Add(info);
                }

                cursor = new UIntPtr(info.BaseAddress.ToUInt64() + info.RegionSize.ToUInt64());
            } while (true);

            return infos;
        }

        /// <summary>
        /// Retrieves information about the memory block the given address lies in.
        /// </summary>
        /// <param name="address">The address of the memory block whose information to retrieve</param>
        /// <returns>The retrieved information</returns>
        public WinAPI.MemoryBasicInformation GetMemoryInfo(UIntPtr address)
        {
            WinAPI.MemoryBasicInformation info = default(WinAPI.MemoryBasicInformation);
            IntPtr ret = WinAPI.VirtualQueryEx(Process.Handle, address, out info, (UIntPtr)Marshal.SizeOf(info));
            if (ret == IntPtr.Zero)
            {
                return default(WinAPI.MemoryBasicInformation);
            }
            return info;
        }

        /// <summary>
        /// Attempts to read a portion of the process' memory into the specified buffer. This method will fail if any
        /// part of the specified memory region is not allowed to be read from.
        /// </summary>
        /// <param name="baseAddress">The address from which to read from</param>
        /// <param name="buffer">The buffer to read the memory into ; must be at least size bytes long</param>
        /// <param name="size">The number of bytes to read ; buffer must be at least size bytes long</param>
        /// <param name="numberOfBytesRead">Returns the number of bytes actually read</param>
        /// <exception cref="ArgumentException">Thrown if buffer is smaller than size bytes</exception>
        /// <exception cref="Win32Exception">Thrown if the underlying WinAPI call fails</exception>
        public void ReadMemory(UIntPtr baseAddress, byte[] buffer, ulong size, out ulong numberOfBytesRead)
        {
            if (size > (ulong) buffer.Length)
            {
                throw new ArgumentException("Invalid buffer size: buffer must be at least size bytes long");
            }

            if (!WinAPI.ReadProcessMemory(Process.Handle, baseAddress, buffer, new UIntPtr(size), out UIntPtr numberOfBytesReadPtr))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to read process memory");
            }

            numberOfBytesRead = numberOfBytesReadPtr.ToUInt64();
        }

        /// <summary>
        /// Attempts to write a portion of the process' memory with data from the specified buffer.
        /// </summary>
        /// <param name="baseAddress">The address at which to write to</param>
        /// <param name="buffer">The buffer from which to copy data</param>
        /// <param name="size">The number of bytes to write</param>
        /// <param name="numberOfBytesWritten">The number of bytes that were actually written</param>
        /// <exception cref="ArgumentException">Thrown if buffer is smaller than size bytes</exception>
        /// <exception cref="Win32Exception">Thrown if the underlying WinAPI call fails</exception>
        public void WriteMemory(UIntPtr baseAddress, byte[] buffer, ulong size, out ulong numberOfBytesWritten)
        {
            if (size > (ulong) buffer.Length)
            {
                throw new ArgumentException("Invalid buffer size: buffer must be at least size bytes long");
            }

            if (!WinAPI.WriteProcessMemory(Process.Handle, baseAddress, buffer, new UIntPtr(size), out UIntPtr numberOfBytesWrittenPtr))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to write process memory");
            }

            numberOfBytesWritten = numberOfBytesWrittenPtr.ToUInt64();
        }

    }
}
