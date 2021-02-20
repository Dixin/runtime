// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<int> Range(int start, int count)
        {
            long max = ((long)start) + count - 1;
            if (count < 0 || max > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            if (count == 0)
            {
                return Empty<int>();
            }

            return new RangeIterator(start, count);
        }

        /// <summary>
        /// An iterator that yields a range of consecutive integers.
        /// </summary>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class RangeIterator : Iterator<int>
        {
            private readonly int _startInclusive;
            private readonly int _endExclusive;

            public RangeIterator(int startInclusive, int count)
            {
                Debug.Assert(count > 0);
                _startInclusive = startInclusive;
                _endExclusive = unchecked(startInclusive + count);
            }

            private int CountForDebugger => _endExclusive - _startInclusive;

            public override Iterator<int> Clone() => new RangeIterator(_startInclusive, _endExclusive - _startInclusive);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        Debug.Assert(_startInclusive != _endExclusive);
                        _current = _startInclusive;
                        _state = 2;
                        return true;
                    case 2:
                        if (unchecked(++_current) == _endExclusive)
                        {
                            break;
                        }

                        return true;
                }

                _state = -1;
                return false;
            }

            public override void Dispose()
            {
                _state = -1; // Don't reset current
            }
        }
    }
}
