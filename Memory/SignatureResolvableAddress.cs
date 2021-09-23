using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM.Memory
{
    public class SignatureResolvableAddress : IResolvableAddress
    {
        private readonly Signature _signature;
        private readonly int _offset;
        private readonly Func<WinAPI.MemoryBasicInformation, bool> _filter;

        /// <summary>
        /// Constructs a new signature-resolvable address given its signature.
        /// </summary>
        /// <param name="signature">The address' unique signature</param>
        /// <param name="offset">The offset into a signature's match at which the actual address may be found</param>
        /// <param name="assembly">Whether or not the signature is expected in an assembly (i.e. executable) memory region</param>
        public SignatureResolvableAddress(Signature signature, int offset, bool assembly = true)
        {
            _signature = signature;
            _offset = offset;
            if (assembly)
            {
                _filter = m => (m.Protect & WinAPI.PAGE_EXECUTE_READWRITE) != 0 && (m.Protect & WinAPI.PAGE_GUARD) == 0;
            }
            else
            {
                _filter = m => m.Readable && m.Writable && (m.Protect & WinAPI.PAGE_GUARD) == 0;
            }

        }
        
        public UIntPtr Resolve(MemoryInterface memInterface)
        {
            MemoryScanner scanner = new MemoryScanner(memInterface);
            List<MemoryScanner.Match> matches = scanner.Find(_signature, _filter, true);
            try
            {
                return matches.First().GetUIntPtr(_offset);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not resolve address by signature", ex);
            }
        }
    }
}
