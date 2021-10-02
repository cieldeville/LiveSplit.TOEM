using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSplit.TOEM.Memory
{
    public class PointerPath
    {
        public class Builder
        {
            private IResolvableAddress _entry;
            private List<IPointerPathElement> _elements = new List<IPointerPathElement>();

            internal Builder(IResolvableAddress entry)
            {
                _entry = entry;
            }

            internal Builder(IResolvableAddress entry, IPointerPathElement[] elements)
            {
                _entry = entry;
                _elements.AddRange(elements);
            }

            public Builder Deref()
            {
                _elements.Add(new PointerPathDeref());
                return this;
            }

            public Builder Offset(ulong offset)
            {
                _elements.Add(new PointerPathOffset(offset));
                return this;
            }

            public PointerPath Build()
            {
                return new PointerPath(_entry, _elements.ToArray());
            }
        }

        private readonly IResolvableAddress _entry;
        private readonly IPointerPathElement[] _elements;
        
        private ulong _cachedAddress;

        private PointerPath(IResolvableAddress entry, IPointerPathElement[] elements)
        {
            _entry = entry;
            _elements = elements;
            _cachedAddress = 0;
        }

        public UIntPtr Follow(MemoryInterface memInterface)
        {
            if (_cachedAddress == 0)
            {
                try
                {
                    ulong cursor = _entry.Resolve(memInterface);
                    foreach (IPointerPathElement element in _elements)
                    {
                        cursor = element.Follow(memInterface, cursor);
                    }
                    _cachedAddress = cursor;
                }
                catch (Exception ex)
                {
                    _cachedAddress = 0;
                    throw new Exception("Could not follow pointer path", ex);
                }
            }
            return new UIntPtr(_cachedAddress);
        }

        public void Flush(bool entry = false)
        {
            if (entry) _entry.Flush();
            _cachedAddress = 0;
        }

        public Builder Extend()
        {
            return new Builder(_entry, _elements);
        }

        public static Builder Signature(Signature signature, int offset, bool assembly = true)
        {
            return new Builder(new SignatureResolvableAddress(signature, offset, assembly));
        }

        public static Builder Module(string moduleName, ulong offset = 0)
        {
            return new Builder(new ModuleResolvableAddress(moduleName, offset));
        }
    }
}
