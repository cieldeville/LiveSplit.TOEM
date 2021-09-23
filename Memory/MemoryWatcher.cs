using System;

namespace LiveSplit.TOEM.Memory
{
    /// <summary>
    /// Utility class which allows for watching a region of a foreign process' memory.
    /// </summary>
    public class MemoryWatcher
    {
        /// <summary>
        /// The contents of the last successful memory snapshot taken during an Update()
        /// </summary>
        public byte[] Content { get { return _contents; } }

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

        private byte[] _contents;

        // Memory Info
        private readonly MemoryInterface _memInterface;
        private readonly UIntPtr _memAddress;
        private readonly int _memSize;
        private byte[] _memBuffer;
        private MemoryAccessFlags _memAccess;

        internal MemoryWatcher(MemoryInterface memInterface, UIntPtr memAddress, int memSize, MemoryAccessFlags memAccess)
        {
            _contents = new byte[memSize];

            _memInterface = memInterface;
            _memAddress = memAddress;
            _memSize = memSize;
            _memBuffer = new byte[memSize];
            _memAccess = memAccess;
        }

        /// <summary>
        /// Attempts to update the watcher's current value by retrieving a new memory snapshot.
        /// If the memory region in question is not readable or a snapshot
        /// could not be taken in full, the update will fail, this method will return false
        /// and the Content property will be left unchanged.
        /// 
        /// If this method succeeds it will return true and Content will reflect the snapshot that
        /// was taken.
        /// </summary>
        /// <returns>True on success, false on failure.</returns>
        public bool Update()
        {
            // No-op, if memory is not readable
            if (!Readable) return false;

            try
            {
                // Retrieve memory snapshot
                _memInterface.ReadMemory(_memAddress, _memBuffer, (ulong)_memSize, out ulong numberOfBytesRead);
                if (numberOfBytesRead != (ulong)_memSize)
                {
                    return false;
                }

                // Transfer contents
                Buffer.BlockCopy(_memBuffer, 0, _contents, _memSize, _memSize);
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
        /// in this case this method will return false.
        /// 
        /// Unlike setting a variable through a VariableWatcher writing to a MemoryWatcher will NOT
        /// update the currently held contents of the watcher. Since it allows to only overwrite parts
        /// of the watched memory region a full new snapshot would need to be taken after the write
        /// to ensure that any part that was not written to did not change since the last call to
        /// Update().
        /// </summary>
        /// <param name="src">The source array to copy from</param>
        /// <param name="srcOffset">The offset into the source array at which to begin copying from</param>
        /// <param name="dstOffset">The offset into the watched memory region at which to begin copying to</param>
        /// <param name="count">The amount of bytes to copy</param>
        /// <returns>Whether or not the operation completed successfully and in whole</returns>
        public bool Write(byte[] src, int srcOffset, int dstOffset, int count)
        {
            if (!Writable) return false;
            if (dstOffset + count > _memSize) return false; // Prevent out-of-bounds writes
            if (srcOffset + count > src.Length) return false; // Prevent out-of-bounds reads

            try
            {
                byte[] buffer;
                if (srcOffset == 0 && count == src.Length) buffer = src;
                else
                {
                    Buffer.BlockCopy(src, srcOffset, _memBuffer, 0, count);
                    buffer = _memBuffer;
                }

                _memInterface.WriteMemory(new UIntPtr(_memAddress.ToUInt64() + (ulong) dstOffset), buffer, (ulong) count, out ulong numberOfBytesWritten);
                if (numberOfBytesWritten != (ulong) count)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                // TODO: Log exception
                return false;
            }

            return true;
        }
    }
}
