using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM.Memory
{
    public interface IResolvableAddress
    {
        /// <summary>
        /// Attempts to resolve this address through the specified memory interface.
        /// </summary>
        /// <param name="memInterface">The memory interface to use for resolution</param>
        /// <returns>The resolved address on success or UIntPtr.Zero on failure</returns>
        /// <exception cref="Exception">Thrown if the address resolution fails</exception>
        UIntPtr Resolve(MemoryInterface memInterface);
    }
}
