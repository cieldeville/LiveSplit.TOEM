using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM
{
    public class SignaturePointerResolution : IPointerResolution
    {
        
        public IntPtr resolve(Process process)
        {
            return IntPtr.Zero;
        }
    }
}
