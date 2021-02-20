// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class RangeIterator : IPartition<int>
        {
            public override IEnumerable<TResult> Select<TResult>(Func<int, TResult> selector)
            {
                return new SelectRangeIterator<TResult>(_startInclusive, _endExclusive, selector);
            }

            public int[] ToArray()
            {
                int[] array = new int[_endExclusive - _startInclusive];
                int cur = _startInclusive;
                for (int i = 0; i != array.Length; ++i)
                {
                    array[i] = cur;
                    ++cur;
                }

                return array;
            }

            public List<int> ToList()
            {
                List<int> list = new List<int>(_endExclusive - _startInclusive);
                for (int cur = _startInclusive; cur != _endExclusive; cur++)
                {
                    list.Add(cur);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap) => unchecked(_endExclusive - _startInclusive);

            public IPartition<int> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new RangeIterator(_startInclusive + normalizedStartIndexInclusive, normalizedEndIndexExclusive - normalizedStartIndexInclusive));

            //public int TryGetElementAt(int index, out bool found)
            //{
            //    if (unchecked((uint)index < (uint)(_endExclusive - _startInclusive)))
            //    {
            //        found = true;
            //        return _startInclusive + index;
            //    }

            //    found = false;
            //    return 0;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out int element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _startInclusive + normalizedIndex, out element);

            //public int TryGetFirst(out bool found)
            //{
            //    found = true;
            //    return _startInclusive;
            //}

            //public int TryGetLast(out bool found)
            //{
            //    found = true;
            //    return _endExclusive - 1;
            //}
        }
    }
}
