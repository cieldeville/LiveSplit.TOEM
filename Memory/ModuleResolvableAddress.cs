using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM.Memory
{
    public class ModuleResolvableAddress : IResolvableAddress
    {
        public string ModuleName { get; }
        public ulong ModuleOffset { get; }

        private ulong _cachedAddress;

        public ModuleResolvableAddress(string moduleName, ulong offset = 0)
        {
            ModuleName = moduleName;
            ModuleOffset = offset;
            Flush();
        }

        public void Flush()
        {
            _cachedAddress = 0;
        }

        public ulong Resolve(MemoryInterface memInterface)
        {
            if (_cachedAddress == 0)
            {
                Module module;
                if (!memInterface.TryGetModule(ModuleName, out module))
                {
                    throw new Exception("Could not resolve address: module '" + ModuleName + "' not found");
                }

                _cachedAddress = module.BaseAddress.ToUInt64() + ModuleOffset;
            }
            return _cachedAddress;
        }
    }
}
