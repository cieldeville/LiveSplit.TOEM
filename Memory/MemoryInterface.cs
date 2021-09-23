using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        /// <summary>
        /// Creates a VariableWatcher which may be used to access a variable in the process' memory.
        /// </summary>
        /// <typeparam name="T">The type of variable ; must be unmanaged and primitive</typeparam>
        /// <param name="address">The address of the variable</param>
        /// <param name="defaultValue">The variable's default value</param>
        /// <returns>A variable watcher for the described variable</returns>
        public VariableWatcher<T> WatchMemory<T>(UIntPtr address, T defaultValue = default(T)) where T : unmanaged
        {
            // Determine access levels
            int size;
            unsafe
            {
                if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr)) size = IntPtr.Size;
                else size = sizeof(T);
            }

            MemoryAccessFlags flags = DetermineAccessFlags(address, size);
            return new VariableWatcher<T>(this, address, flags, defaultValue);
        }

        /// <summary>
        /// See overload for UIntPtr address.
        /// </summary>
        public VariableWatcher<T> WatchMemory<T>(IResolvableAddress address, T defaultValue = default(T)) where T : unmanaged
        {
            return WatchMemory(address.Resolve(this), defaultValue);
        }

        /// <summary>
        /// Creates a MemoryWatcher which may be used to access a region of the process' memory.
        /// </summary>
        /// <param name="address">The address of the memory region</param>
        /// <param name="count">The size of the memory region in bytes</param>
        /// <returns>A memory watcher for the described memory region</returns>
        public MemoryWatcher WatchMemory(UIntPtr address, int count)
        {
            MemoryAccessFlags flags = DetermineAccessFlags(address, count);
            return new MemoryWatcher(this, address, count, flags);
        }

        /// <summary>
        /// See overload for UIntPtr address.
        /// </summary>
        public MemoryWatcher WatchMemory(IResolvableAddress address, int count)
        {
            return WatchMemory(address.Resolve(this), count);
        }

        private MemoryAccessFlags DetermineAccessFlags(UIntPtr address, int size)
        {
            MemoryAccessFlags flags = MemoryAccessFlags.Read | MemoryAccessFlags.Write | MemoryAccessFlags.Execute;

            ulong cursor = address.ToUInt64();
            ulong end = cursor + (ulong)size;
            while (cursor < end)
            {
                WinAPI.MemoryBasicInformation memInfo = GetMemoryInfo(new UIntPtr(cursor));
                flags &= memInfo.Readable ? ~MemoryAccessFlags.None : ~MemoryAccessFlags.Read;
                flags &= memInfo.Writable ? ~MemoryAccessFlags.None : ~MemoryAccessFlags.Write;
                flags &= memInfo.Executable ? ~MemoryAccessFlags.None : ~MemoryAccessFlags.Execute;
                cursor += memInfo.RegionSize.ToUInt64();
            }

            return flags;
        }

    }
}
