using System;

namespace LiveSplit.TOEM.Memory
{
    /// <summary>
    /// Helper class for watching a variable within a foreign process' memory.
    /// </summary>
    /// <typeparam name="T">The type of the variable ; must be an unmanaged, primitve type</typeparam>
    public class VariableWatcher<T> where T : unmanaged
    {
        /// <summary>
        /// The variable's value retrieved before the last update
        /// </summary>
        public T OldValue { get { return _oldValue; } }
        /// <summary>
        /// The variable's current value as retrieved during the last update
        /// </summary>
        public T CurrentValue { get { return _currentValue; } }

        /// <summary>
        /// A set of flags describing the available access to this variable.
        /// </summary>
        public MemoryAccessFlags AccessFlags { get { return _memAccess; } }
        /// <summary>
        /// Convenience property for checking if this variable may be read.
        /// </summary>
        public bool Readable { get { return (_memAccess & MemoryAccessFlags.Read) != MemoryAccessFlags.None; } }
        /// <summary>
        /// Convenience property for checking if this variable may be written.
        /// </summary>
        public bool Writable { get { return (_memAccess & MemoryAccessFlags.Write) != MemoryAccessFlags.None; } }

        // Variable state
        private T _oldValue;
        private T _currentValue;
        private readonly T _defaultValue;

        // Memory Info
        private readonly MemoryInterface _memInterface;
        private readonly PointerPath _memPath;
        private readonly int _memSize;
        private byte[] _memBuffer;
        private MemoryAccessFlags _memAccess;

        internal VariableWatcher(MemoryInterface memInterface, PointerPath memPath, MemoryAccessFlags memAccess, T defaultValue = default)
        {
            _oldValue = defaultValue;
            _currentValue = defaultValue;
            _defaultValue = defaultValue;

            _memInterface = memInterface;
            _memPath = memPath;
            unsafe
            {
                if (typeof(T) == typeof(IntPtr) || typeof(T) == typeof(UIntPtr)) _memSize = IntPtr.Size;
                else _memSize = sizeof(T);
            }
            _memBuffer = new byte[_memSize];
            _memAccess = memAccess;
        }

        /// <summary>
        /// Attempts to update the watcher's current value by retrieving a new snapshot of the
        /// variable's memory. If the memory region in question is not readable or a snapshot
        /// could not be taken in full, the update will fail, this method will return false
        /// and the OldValue and CurrentValue properties will be left unchanged.
        /// 
        /// If this method succeeds it will return true, OldValue will receive the value held
        /// by CurrentValue and CurrentValue will be set to the value freshly retrieved.
        /// </summary>
        /// <returns>True on success, false on failure.</returns>
        public bool Update()
        {
            // No-op, if memory is not readable
            if (!Readable) return false;

            try
            {
                // Retrieve memory snapshot
                _memInterface.ReadMemory(_memPath.Follow(_memInterface), _memBuffer, (ulong) _memSize, out ulong numberOfBytesRead);
                if (numberOfBytesRead != (ulong) _memSize)
                {
                    return false;
                }

                // Cast buffer to type
                _oldValue = _currentValue;
                unsafe
                {
                    fixed (byte* ptr = _memBuffer)
                    {
                        _currentValue = *(T*)ptr;
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Log exception
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to write the specified value into the variable's watched memory region. This
        /// requires the memory region to be writable. There is a chance of a partial write occurring ;
        /// in this case this method will return false and CurrentValue and OldValue will not reflect
        /// the specified value.
        /// </summary>
        /// <param name="value">The value to set the variable to</param>
        /// <returns>Whether or not the operation completed successfully</returns>
        public bool Set(T value)
        {
            if (!Writable) return false;

            try
            {
                unsafe
                {
                    fixed (byte* ptr = _memBuffer)
                    {
                        Buffer.MemoryCopy(&value, ptr, _memSize, _memSize);
                    }
                }

                _memInterface.WriteMemory(_memPath.Follow(_memInterface), _memBuffer, (ulong)_memSize, out ulong numberOfBytesWritten);
                if (numberOfBytesWritten != (ulong) _memSize)
                {
                    return false;
                }

                _oldValue = _currentValue;
                _currentValue = value;
            }
            catch (Exception)
            {
                // TODO: Log exception
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "{ old = " + OldValue + ", current = " + CurrentValue + "}";
        }
    }
}
