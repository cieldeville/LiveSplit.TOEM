using System;

namespace LiveSplit.TOEM.Memory
{
    public class MemoryWatcher
    {
        public enum ProtectionFlags : int
        {
            None = 0,
            Readable = 1,
            Writable = 2,
            Executable = 4
        }

        public MemoryInterface MemoryInterface { get; }
        public UIntPtr Address { get; }
        public int Size { get; }
        public bool Dirty { get { return _dirty; } }

        public ProtectionFlags Flags { get { return _flags; } }

        private byte[] _buffer;
        private ProtectionFlags _flags;
        private bool _dirty;

        public MemoryWatcher(MemoryInterface memory, UIntPtr address, int size)
        {
            MemoryInterface = memory;
            Address = address;
            Size = size;

            _buffer = new byte[size];
            _flags = ProtectionFlags.None;
            _dirty = true;

            Initialize();
        }

        /// <summary>
        /// Attempts to update the memory watcher with the current contents of the watched memory region.
        /// 
        /// Should the procedure fail, the watcher will be left in a dirty state.
        /// </summary>
        /// <returns>Whether or not the operation completed successfully</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the watched memory region is not readable</exception>
        public bool Update()
        {
            if ((_flags & ProtectionFlags.Readable) == 0) throw new UnauthorizedAccessException("Memory region is not readable");
            try
            {
                MemoryInterface.ReadMemory(Address, _buffer, (ulong)Size, out ulong numberOfBytesRead);
                return !(_dirty = (numberOfBytesRead != (ulong)Size));
            }
            catch (Exception ex)
            {
                // TODO: Log exception
                _dirty = true;
                return false;
            }
        }

        /// <summary>
        /// Gets the data of this memory region as it was when last updated.
        /// 
        /// In case the watcher is currently in a dirty state (e.g. the last update only completed partially)
        /// an update will be triggered.
        /// </summary>
        /// <returns>The contents of the watched memory region on the last update</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the watched memory region is not readable</exception>
        /// <exception cref="Exception">Thrown if the watcher was in a dirty state and could not be updated successfully</exception>
        public byte[] GetRaw()
        {
            if ((_flags & ProtectionFlags.Readable) == 0) throw new UnauthorizedAccessException("Memory region is not readable");
            if (_dirty && !Update()) throw new Exception("Could not retrieve data from memory");
            return _buffer;
        }

        /// <summary>
        /// Gets the data of this memory region and converts it to some primitive type. The size of the primitive type and
        /// the watched memory region must match. Internally GetRaw() will be invoked to retrieve the contents, so please
        /// refer to its documentation as well.
        /// </summeary>
        /// <typeparam name="T">The primitive type (or IntPtr / UIntPtr) to convert to</typeparam>
        /// <exception cref="ArgumentException">Thrown if the primitive type's size does not match the size of the memory region</exception>
        /// <returns>The converted value on success</returns>
        public T Get<T>() where T : unmanaged
        {
            unsafe
            {
                int s = 0;
                if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr)) s = IntPtr.Size;
                else s = sizeof(T);
                
                if (s != Size) throw new ArgumentException("Incompatible type size");

                byte[] raw = GetRaw();
                fixed (byte* p = raw)
                {
                    return *(T*)p;
                }
            }
        }

        /// <summary>
        /// Attempts to set the watched memory region's contents.
        /// 
        /// Should the write operation to the watched memory region not complete successfully the watcher will be left in a dirty state.
        /// </summary>
        /// <param name="value">A byte array with at least Size bytes containing the new memory contents</param>
        /// <returns>Whether or not the memory region could be written to successfully</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the watched memory region is not writable</exception>
        /// <exception cref="ArgumentException">Thrown if value is not at least Size bytes long</exception>
        public bool SetRaw(byte[] value)
        {
            if (value.Length < Size) throw new ArgumentException("Must provide at least Size bytes of data");
            if ((_flags & ProtectionFlags.Writable) == 0) throw new UnauthorizedAccessException("Memory region is not writable");

            try
            {
                MemoryInterface.WriteMemory(Address, value, (ulong) Size, out ulong numberOfBytesWritten);
                _dirty = (numberOfBytesWritten != (ulong)Size);
                Buffer.BlockCopy(value, 0, _buffer, 0, Size);
                return !_dirty;
            }
            catch (Exception e)
            {
                // TODO: Log exception
                _dirty = true;
                return false;
            }
        }

        /// <summary>
        /// Attempts to replace the watched memory region's contents with the specified value.
        /// 
        /// The value must be a primitive type or IntPtr / UIntPtr whose size matches the watched memory's region exactly. Internally,
        /// SetRaw will be invoked so please also refer to its documentation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value to set</param>
        /// <returns>Whether or not the memory region could be updated successfully</returns>
        public bool Set<T>(T value) where T : unmanaged
        {                
            byte[] temp;
            unsafe
            {
                int size = 0;
                if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr)) size = IntPtr.Size;
                else size = sizeof(T);

                if (size != Size) throw new ArgumentException("Incompatible type size");

                temp = new byte[size];
                fixed (byte* p = temp)
                {
                    Buffer.MemoryCopy(&value, p, size, size);
                }
            }
            return SetRaw(temp);
        }

        private void Initialize()
        {
            WinAPI.MemoryBasicInformation memInfo = MemoryInterface.GetMemoryInfo(Address);

            _flags = ProtectionFlags.None;
            _flags |= memInfo.Readable ? ProtectionFlags.Readable : ProtectionFlags.None;
            _flags |= memInfo.Writable ? ProtectionFlags.Writable : ProtectionFlags.None;
            _flags |= memInfo.Executable ? ProtectionFlags.Executable : ProtectionFlags.None;
        }

    }
}
