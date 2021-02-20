// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    internal abstract partial class OrderedEnumerable<TElement> : IPartition<TElement>
    {
        public TElement[] ToArray()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(_source);

            int count = buffer._count;
            if (count == 0)
            {
                return buffer._items;
            }

            TElement[] array = new TElement[count];
            int[] map = SortedMap(buffer);
            for (int i = 0; i != array.Length; i++)
            {
                array[i] = buffer._items[map[i]];
            }

            return array;
        }

        public List<TElement> ToList()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(_source);
            int count = buffer._count;
            List<TElement> list = new List<TElement>(count);
            if (count > 0)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i != count; i++)
                {
                    list.Add(buffer._items[map[i]]);
                }
            }

            return list;
        }

        public int GetCount(bool onlyIfCheap)
        {
            if (_source is IIListProvider<TElement> listProv)
            {
                return listProv.GetCount(onlyIfCheap);
            }

            return !onlyIfCheap || _source is ICollection<TElement> || _source is ICollection ? _source.Count() : -1;
        }

        internal TElement[] ToArray(int minIndexInclusive, int maxIndexInclusive)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(_source);
            int count = buffer._count;
            if (count <= minIndexInclusive)
            {
                return Array.Empty<TElement>();
            }

            if (count <= maxIndexInclusive)
            {
                maxIndexInclusive = count - 1;
            }

            if (minIndexInclusive == maxIndexInclusive)
            {
                return new TElement[] { GetEnumerableSorter().ElementAt(buffer._items, count, minIndexInclusive) };
            }

            int[] map = SortedMap(buffer, minIndexInclusive, maxIndexInclusive);
            TElement[] array = new TElement[maxIndexInclusive - minIndexInclusive + 1];
            int idx = 0;
            while (minIndexInclusive <= maxIndexInclusive)
            {
                array[idx] = buffer._items[map[minIndexInclusive]];
                ++idx;
                ++minIndexInclusive;
            }

            return array;
        }

        internal List<TElement> ToList(int minIndexInclusive, int maxIndexInclusive)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(_source);
            int count = buffer._count;
            if (count <= minIndexInclusive)
            {
                return new List<TElement>();
            }

            if (count <= maxIndexInclusive)
            {
                maxIndexInclusive = count - 1;
            }

            if (minIndexInclusive == maxIndexInclusive)
            {
                return new List<TElement>(1) { GetEnumerableSorter().ElementAt(buffer._items, count, minIndexInclusive) };
            }

            int[] map = SortedMap(buffer, minIndexInclusive, maxIndexInclusive);
            List<TElement> list = new List<TElement>(maxIndexInclusive - minIndexInclusive + 1);
            while (minIndexInclusive <= maxIndexInclusive)
            {
                list.Add(buffer._items[map[minIndexInclusive]]);
                ++minIndexInclusive;
            }

            return list;
        }

        internal int GetCount(int minIndexInclusive, int maxIndexInclusive, bool onlyIfCheap)
        {
            int count = GetCount(onlyIfCheap);
            if (count <= 0)
            {
                return count;
            }

            if (count <= minIndexInclusive)
            {
                return 0;
            }

            return (count <= maxIndexInclusive ? count : maxIndexInclusive + 1) - minIndexInclusive;
        }

        public IPartition<TElement> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
            this.ToPartition(startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd);

        public IPartition<TElement> Skip(int count) => new OrderedPartition<TElement>(this, count, int.MaxValue);

        public IPartition<TElement> Take(int count) => new OrderedPartition<TElement>(this, 0, count - 1);

        public TElement? TryGetElementAt(int index, out bool found)
        {
            if (index == 0)
            {
                return TryGetFirst(out found);
            }

            if (index > 0)
            {
                Buffer<TElement> buffer = new Buffer<TElement>(_source);
                int count = buffer._count;
                if (index < count)
                {
                    found = true;
                    return GetEnumerableSorter().ElementAt(buffer._items, count, index);
                }
            }

            found = false;
            return default;
        }

        public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TElement element)
        {
            if (index == 0)
            {
                if (isIndexFromEnd)
                {
                    element = TryGetLast(out bool found);
                    return found;
                }
                else
                {
                    element = TryGetFirst(out bool found);
                    return found;
                }
            }

            if (index > 0)
            {
                Buffer<TElement> buffer = new(_source);
                int count = buffer._count;
                if (isIndexFromEnd)
                {
                    index = count - index;
                }

                if (index >= 0 && index < count)
                {
                    element = GetEnumerableSorter().ElementAt(buffer._items, count, index);
                    return true;
                }
            }

            element = default;
            return false;
        }

        public TElement? TryGetFirst(out bool found)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = _source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    found = false;
                    return default;
                }

                TElement value = e.Current;
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (comparer.Compare(x, true) < 0)
                    {
                        value = x;
                    }
                }

                found = true;
                return value;
            }
        }

        public TElement? TryGetLast(out bool found)
        {
            using (IEnumerator<TElement> e = _source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    found = false;
                    return default;
                }

                CachingComparer<TElement> comparer = GetComparer();
                TElement value = e.Current;
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement current = e.Current;
                    if (comparer.Compare(current, false) >= 0)
                    {
                        value = current;
                    }
                }

                found = true;
                return value;
            }
        }

        public TElement? TryGetLast(int minIndexInclusive, int maxIndexInclusive, out bool found)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(_source);
            int count = buffer._count;
            if (minIndexInclusive >= count)
            {
                found = false;
                return default;
            }

            found = true;
            return (maxIndexInclusive < count - 1) ? GetEnumerableSorter().ElementAt(buffer._items, count, maxIndexInclusive) : Last(buffer);
        }

        private TElement Last(Buffer<TElement> buffer)
        {
            CachingComparer<TElement> comparer = GetComparer();
            TElement[] items = buffer._items;
            int count = buffer._count;
            TElement value = items[0];
            comparer.SetElement(value);
            for (int i = 1; i != count; ++i)
            {
                TElement x = items[i];
                if (comparer.Compare(x, false) >= 0)
                {
                    value = x;
                }
            }

            return value;
        }
    }
}
