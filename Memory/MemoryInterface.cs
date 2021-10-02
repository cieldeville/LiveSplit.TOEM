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
        /// The process' modules at the time of the interface's construction.
        /// </summary>
        public List<Module> Modules { get { return _modules; } }

        private List<Module> _modules;
        private Dictionary<string, Module> _modulesByName;

        /// <summary>
        /// Constructs a new memory interface to access the specified process' memory.
        /// </summary>
        /// <param name="process">The process whose memory to access</param>
        public MemoryInterface(Process process)
        {
            Process = process;
            EnumerateModules();
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
        /// Attempts to retrieve a module given it's name (case-insensitive)
        /// </summary>
        /// <param name="name">The module's name (case-insensitive)</param>
        /// <param name="module">The module itself, if found</param>
        /// <returns>True if the module was found, false otherwise</returns>
        public bool TryGetModule(string name, out Module module)
        {
            return _modulesByName.TryGetValue(name, out module);
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
        /// <param name="path">The variable's pointer path</param>
        /// <param name="defaultValue">The variable's default value</param>
        /// <returns>A variable watcher for the described variable</returns>
        public VariableWatcher<T> WatchMemory<T>(PointerPath path, T defaultValue = default(T)) where T : unmanaged
        {
            // Determine access levels
            int size;
            unsafe
            {
                if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr)) size = IntPtr.Size;
                else size = sizeof(T);
            }

            MemoryAccessFlags flags = DetermineAccessFlags(path.Follow(this), size);
            return new VariableWatcher<T>(this, path, flags, defaultValue);
        }

        /// <summary>
        /// Creates a MemoryWatcher which may be used to access a region of the process' memory.
        /// </summary>
        /// <param name="path">The memory region's pointer path</param>
        /// <param name="count">The size of the memory region in bytes</param>
        /// <returns>A memory watcher for the described memory region</returns>
        public MemoryWatcher WatchMemory(PointerPath path, int count)
        {
            MemoryAccessFlags flags = DetermineAccessFlags(path.Follow(this), count);
            return new MemoryWatcher(this, path, count, flags);
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

        private void EnumerateModules()
        {
            UIntPtr[] moduleHandles;
            uint cb;
            uint cbNeeded = 32;

            do
            {
                cb = cbNeeded;
                moduleHandles = new UIntPtr[cb / UIntPtr.Size];

                if (!WinAPI.EnumProcessModulesEx(Process.Handle, moduleHandles, (uint) (moduleHandles.Length * UIntPtr.Size), out cbNeeded, WinAPI.LIST_MODULES_ALL))
                {
                    return;
                }
            } while (cbNeeded > cb);

            int numModules = (int) cbNeeded / UIntPtr.Size;
            _modules = new List<Module>(numModules);
            _modulesByName = new Dictionary<string, Module>(numModules, StringComparer.OrdinalIgnoreCase);

            char[] nameBuffer = new char[WinAPI.MAX_PATH];

            for (int i = 0; i < numModules; ++i)
            {
                uint ret = WinAPI.GetModuleBaseName(Process.Handle, moduleHandles[i], nameBuffer, WinAPI.MAX_PATH);
                if (ret == 0)
                {
                    return;
                }

                string moduleName = new string(nameBuffer, 0, (int)ret);
                Module module = new Module() { BaseName = moduleName, BaseAddress = moduleHandles[i] };
                _modules.Add(module);
                _modulesByName.Add(moduleName, module);
            }
        }

    }
}
