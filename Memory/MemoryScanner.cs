using System;
using System.Collections.Generic;

namespace LiveSplit.TOEM.Memory
{
    /// <summary>
    /// Utility class which is able to efficiently scan a different process' memory given a signature of bytes to work for.
    /// The signature may contain any number of wildcards. Care must be taken in choosing its view size, i.e. its internal
    /// buffer size. The class will allocate up to twice the amount memory of the chosen view size and requires that its
    /// view size is at least twice the length of any signature that is to be detected.
    /// </summary>
    public class MemoryScanner
    {
        /// <summary>
        /// A data structure that describes a signature match.
        /// </summary>
        public struct Match
        {
            /// <summary>
            /// The address at which the match was found.
            /// </summary>
            public UIntPtr Address { get; }

            /// <summary>
            /// The data in memory that matched a given signature.
            /// </summary>
            public byte[] Data { get; }

            public Match(UIntPtr address, byte[] data)
            {
                Address = address;
                Data = data;
            }

            public int GetInt32(int offset)
            {
                if (offset + 4 > Data.Length) throw new ArgumentException("Attempting to read beyond match boundaries");
                return BitConverter.ToInt32(Data, offset);
            }

            public long GetInt64(int offset)
            {
                if (offset + 8 > Data.Length) throw new ArgumentException("Attempting to read beyond match boundaries");
                return BitConverter.ToInt64(Data, offset);
            }

            public uint GetUInt32(int offset)
            {
                if (offset + 4 > Data.Length) throw new ArgumentException("Attempting to read beyond match boundaries");
                return BitConverter.ToUInt32(Data, offset);
            }

            public ulong GetUInt64(int offset)
            {
                if (offset + 8 > Data.Length) throw new ArgumentException("Attempting to read beyond match boundaries");
                return BitConverter.ToUInt64(Data, offset);
            }

            public IntPtr GetIntPtr(int offset)
            {
                if (offset + IntPtr.Size > Data.Length) throw new ArgumentException("Attempting to read beyond match boundaries");
                if (IntPtr.Size == 4) return new IntPtr(BitConverter.ToInt32(Data, offset));
                else if (IntPtr.Size == 8) return new IntPtr(BitConverter.ToInt64(Data, offset));
                return IntPtr.Zero;
            }

            public UIntPtr GetUIntPtr(int offset)
            {
                if (offset + UIntPtr.Size > Data.Length) throw new ArgumentException("Attempting to read beyond match boundaries");
                if (UIntPtr.Size == 4) return new UIntPtr(BitConverter.ToUInt32(Data, offset));
                else if (UIntPtr.Size == 8) return new UIntPtr(BitConverter.ToUInt64(Data, offset));
                return UIntPtr.Zero;
            }
        }

        private MemoryInterface _interface;
        private byte[] _buffer;
        private byte[] _memoryView;
        private int _available;

        /// <summary>
        /// Constructs a new memory scanner.
        /// </summary>
        /// <param name="memoryInterface">The memory interface to use for accessing foreign memory</param>
        /// <param name="viewSize">The scanner's view size (see class description)</param>
        public MemoryScanner(MemoryInterface memoryInterface, int viewSize = 262144)
        {
            _interface = memoryInterface;
            _memoryView = new byte[viewSize];
        }

        /// <summary>
        /// Attempts to find matches for the given signature in the foreign process' memory.
        /// </summary>
        /// <param name="signature">The signature to search for</param>
        /// <param name="filter">A filter to use for narrowing the search down to specific blocks of memory</param>
        /// <param name="abortAfterMatch">Whether or not the search should abort once a first match is found</param>
        /// <returns>A list of matches found until the search concluded</returns>
        public List<Match> Find(Signature signature, Func<WinAPI.MemoryBasicInformation, bool> filter = null, bool abortAfterMatch = false)
        {
            if (_memoryView.Length < signature.Length * 2)
            {
                throw new ArgumentException("Insufficient view size to perform Find with the given signature");
            }

            _buffer = new byte[_memoryView.Length - signature.Length];

            List<Match> result = new List<Match>();

            int reserved = signature.Length - 1;

            List<WinAPI.MemoryBasicInformation> memInfos = _interface.GetMemoryInfo(filter);
            foreach (WinAPI.MemoryBasicInformation memInfo in memInfos)
            {
                ulong offset = 0;
                ulong numberOfBytesRead = 0;
                ulong baseAddress = memInfo.BaseAddress.ToUInt64();

                while (offset < memInfo.RegionSize.ToUInt64())
                {
                    ReadIntoBuffer(memInfo, offset, reserved, out numberOfBytesRead);
                    List<Match> matches = FindInBlock(signature, baseAddress + offset - (ulong) reserved, abortAfterMatch);
                    offset += numberOfBytesRead;

                    result.AddRange(matches);

                    if (abortAfterMatch && matches.Count > 0)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Reads the next block of the foreigh process' memory into view.
        /// 
        /// The reserved parameter specifies the amount of bytes that will be carried over from the current memory view
        /// into the one after new memory has been read. Since a match could possibly lie exactly on the bounds of
        /// two consecutive views it is vital to choose this parameter according to the length of the signature that
        /// is being searched for.
        /// 
        /// After this operation completes, _memoryView will have the following layout:
        /// +-----------------------+-----------+
        /// | old contents          |  reserved |
        /// +-----------------------+-----------+
        ///                  |
        ///                  |
        ///                  v
        /// +----------+------------------------+
        /// | reserved | new contents           |
        /// +----------+------------------------+
        /// </summary>
        /// <param name="memInfo">The descriptor of the block to load a portion of</param>
        /// <param name="offset">An offset into the block at which to begin loading</param>
        /// <param name="reserved">The amount of bytes from the end of the current view to transfer into the beginning of the next view</param>
        /// <param name="numberOfBytesRead">The number of bytes actually transferred.</param>
        private void ReadIntoBuffer(WinAPI.MemoryBasicInformation memInfo, ulong offset, int reserved, out ulong numberOfBytesRead)
        {
            // Save potential signature at end  and move to front
            Buffer.BlockCopy(_memoryView, _memoryView.Length - reserved, _memoryView, 0, reserved);

            // Downcast is safe since _buffer.Length will always be in int-range
            int size = (int) Math.Min((ulong)(_buffer.Length ), memInfo.RegionSize.ToUInt64() - offset);
            UIntPtr address = new UIntPtr(memInfo.BaseAddress.ToUInt64() + offset);
            _interface.ReadMemory(address, _buffer, (ulong) size, out numberOfBytesRead);

            // Copy new bytes into memory view
            int available = (int)numberOfBytesRead; // Downcast is again safe since it will not exceed size
            Buffer.BlockCopy(_buffer, 0, _memoryView, reserved, available);

            // Update available count
            _available = reserved + available;
        }

        //
        // Knuth-Morris-Pratt with wildcards
        //


        /// <summary>
        /// Searches for a signature match within the chunk of memory currently loaded into view.
        /// </summary>
        /// <param name="signature">The signature to search for</param>
        /// <param name="filter">A filter to use for narrowing the search down to specific blocks of memory</param>
        /// <param name="abortAfterMatch">Whether or not the search should abort once a first match is found</param>
        /// <returns>A list of matches found within the current block of memory</returns>
        private List<Match> FindInBlock(Signature signature, ulong baseAddress, bool abortAfterMatch)
        {
            List<Match> matches = new List<Match>();

            int i = 0;
            int j = 0;

            while (i < _available)
            {
                if (i < _available && (signature.Mask[j] || _memoryView[i] == signature.Bytes[j]))
                {
                    ++i;
                    ++j;

                    if (j == signature.Length)
                    {
                        // Copy data for match
                        byte[] copy = new byte[signature.Length];
                        Buffer.BlockCopy(_memoryView, i - signature.Length, copy, 0, signature.Length);
                        Match match = new Match(new UIntPtr(baseAddress + (ulong) i - (ulong) signature.Length), copy);

                        matches.Add(match);
                        if (abortAfterMatch)
                        {
                            return matches;
                        }

                        // Reset for next match
                        j = signature.Prefixes[j];
                    }
                }
                else
                {
                    j = signature.Prefixes[j];
                    if (j < 0)
                    {
                        ++i;
                        ++j;
                    }
                }
            }

            return matches;
        }



        //
        // Brute Force Search
        //

        //private List<Match> FindInBlock(Signature signature, bool abortAfterMatch)
        //{
        //    List<Match> matches = new List<Match>();

        //    int i = 0;
        //    int j = 0;

        //    while (i < _available)
        //    {
        //        while (i + j < _available && (signature.Mask[j] || _memoryView[i + j] == signature.Bytes[j]))
        //        {
        //            ++j;

        //            if (j == signature.Length)
        //            {
        //                // Match found:

        //                // Copy data for match
        //                byte[] copy = new byte[signature.Length];
        //                Buffer.BlockCopy(_memoryView, i - signature.Length, copy, 0, signature.Length);
        //                Match match = new Match(copy);

        //                matches.Add(match);
        //                if (abortAfterMatch)
        //                {
        //                    return matches;
        //                }

        //                break;
        //            }
        //        }
        //        ++i;
        //        j = 0;
        //    }

        //    return matches;
        //}
    }
}
