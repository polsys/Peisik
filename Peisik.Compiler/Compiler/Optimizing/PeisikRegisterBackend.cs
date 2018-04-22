using System;
using System.Collections.Generic;

namespace Polsys.Peisik.Compiler.Optimizing
{
    /// <summary>
    /// Register backend for Peisik bytecode.
    /// </summary>
    internal class PeisikRegisterBackend : RegisterBackend
    {
        private List<int> _freeBools = new List<int>();
        private List<int> _freeInts = new List<int>();
        private List<int> _freeReals = new List<int>();

        private Dictionary<int, PrimitiveType> _liveTypes = new Dictionary<int, PrimitiveType>();
        private int _nextSlotIndex = 0;

        public override int GetLocation(PrimitiveType type, bool isParameter, out bool onStack)
        {
            // There is no special handling for parameters, because they're assumed
            // to all be alive before any other intervals.
            // We also always set onStack to false, because there is no separate stack.
            // All stack slots are considered registers.

            // Try to get a free location
            // If there are no free slots, create a new one
            var slot = -1;
            switch (type)
            {
                case PrimitiveType.Bool:
                    if (_freeBools.Count > 0)
                    {
                        slot = _freeBools[_freeBools.Count - 1];
                        _freeBools.RemoveAt(_freeBools.Count - 1);
                    }
                    else
                    {
                        slot = _nextSlotIndex;
                        _nextSlotIndex++;
                    }
                    break;
                case PrimitiveType.Int:
                    if (_freeInts.Count > 0)
                    {
                        slot = _freeInts[_freeInts.Count - 1];
                        _freeInts.RemoveAt(_freeInts.Count - 1);
                    }
                    else
                    {
                        slot = _nextSlotIndex;
                        _nextSlotIndex++;
                    }
                    break;
                case PrimitiveType.Real:
                    if (_freeReals.Count > 0)
                    {
                        slot = _freeReals[_freeReals.Count - 1];
                        _freeReals.RemoveAt(_freeReals.Count - 1);
                    }
                    else
                    {
                        slot = _nextSlotIndex;
                        _nextSlotIndex++;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown parameter type");
            }

            _liveTypes.Add(slot, type);
            onStack = false;
            return slot;
        }

        public override void ReturnLocation(int location)
        {
            var type = _liveTypes[location];
            _liveTypes.Remove(location);

            switch (type)
            {
                case PrimitiveType.Bool:
                    _freeBools.Add(location);
                    break;
                case PrimitiveType.Int:
                    _freeInts.Add(location);
                    break;
                case PrimitiveType.Real:
                    _freeReals.Add(location);
                    break;
            }
        }
    }
}
