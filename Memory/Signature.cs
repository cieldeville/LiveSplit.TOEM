using System;
using System.Text.RegularExpressions;

namespace LiveSplit.TOEM.Memory
{
    /// <summary>
    /// A byte signature with optional wildcards.
    /// </summary>
    public class Signature
    {
        /// <summary>
        /// The length of the signature in bytes
        /// </summary>
        public int Length { get { return _sig.Length; } }

        public byte[] Bytes { get { return _sig; } }
        public bool[] Mask { get { return _mask; } }
        public int[] Prefixes { get { return _prefix; } }

        private byte[] _sig;
        private bool[] _mask;
        private int[] _prefix;

        private Signature(byte[] sig, bool[] mask)
        {
            _sig = sig;
            _mask = mask;
            ConstructPrefixTable();
        }

        private void ConstructPrefixTable()
        {
            _prefix = new int[_sig.Length + 1];
            _prefix[0] = -1;

            int i = 1, j = 0;
            while (i < _sig.Length)
            {
                // match
                if (_sig[i] == _sig[j] || _mask[i])
                {
                    ++j;
                    ++i;
                    _prefix[i] = j;
                }
                // mismatch
                else if (j > 0)
                {
                    j = _prefix[j];
                }
                else
                {
                    ++i;
                    _prefix[i] = 0;
                }
            }
        }

        //    F ? A
        // -1 0 1 2
        //
        //   F T T P A
        //       
        //
        //    a b ? a b
        // -1 0 0 1 1 2
        //
        // 

        /// <summary>
        /// Constructs a signature from a pattern such as "A4 B2 ?? ?? EF". The '?' character corresponds to a wildcard character which may represent any byte available.
        /// </summary>
        /// <param name="pattern">The pattern to be converted into a signature</param>
        /// <returns>The construted signature on success</returns>
        /// <exception cref="ArgumentException">Thrown if an invalid pattern was specified</exception>
        public static Signature From(params string[] pattern)
        {
            string combinedPattern = Regex.Replace(string.Concat(pattern), @"\s", "");
            if (combinedPattern.Length % 2 != 0) throw new ArgumentException("Pattern cannot contain half-bytes");

            int length = (combinedPattern.Length + 1) >> 1;

            byte[] sig = new byte[length];
            bool[] mask = new bool[length];

            int cursor = 0;


            for (int i = 0; i < combinedPattern.Length; i += 2)
            {
                char c = combinedPattern[i];
                if (!IsHexDigit(c) && c != '?') throw new ArgumentException("Pattern includes invalid characters");
                
                if (c == '?')
                {
                    if (combinedPattern[i + 1] != '?')
                    {
                        throw new ArgumentException("Cannot wildcard half-bytes");
                    }

                    mask[cursor] = true;
                    sig[cursor] = 0;
                }
                else
                {
                    mask[cursor] = false;

                    int hi = HexDigitValue(c);
                    int lo = HexDigitValue(combinedPattern[i + 1]);

                    sig[cursor] = (byte)(hi << 4 | lo);
                }

                ++cursor;
            }

            return new Signature(sig, mask);
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || (c >= '0' && c <= '9');
        }

        private static int HexDigitValue(char c)
        {
            if (c >= 'a') return ((c - 'a') + 0xA);
            else if (c >= 'A') return ((c - 'A') + 0xA);
            else return (c - '0');
        }
    }
}
