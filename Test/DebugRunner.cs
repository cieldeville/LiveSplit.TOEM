using LiveSplit.TOEM.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSplit.TOEM.Test
{
    class DebugRunner
    {
        public static void Main(string[] args)
        {
            //Signature sig = Signature.From("41 FF D3 48 8B CE 48 B8 ?? ?? ?? ?? ?? ?? ?? ?? 89 08 48 B8");

            TOEMComponent component = new TOEMComponent(null);

            while (true)
            {
                Thread.Sleep(10);
            }
        }
    }
}
