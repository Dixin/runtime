// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    /// <summary>
    /// Represents an enumerable with zero elements.
    /// </summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <remarks>
    /// Returning an instance of this type is useful to quickly handle scenarios where it is known
    /// that an operation will result in zero elements.
    /// </remarks>
    [DebuggerDisplay("Count = 0")]
    internal sealed class EmptyPartition<TElement> : IPartition<TElement>, IEnumerator<TElement>
    {
        /// <summary>
        /// A cached, immutable instance of an empty enumerable.
        /// </summary>
        public static readonly IPartition<TElement> Instance = new EmptyPartition<TElement>();

        private EmptyPartition()
        {
        }

        public IEnumerator<TElement> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;

        public bool MoveNext() => false;

        [ExcludeFromCodeCoverage(Justification = "Shouldn't be called, and as undefined can return or throw anything anyway")]
        public TElement Current => default!;

        [ExcludeFromCodeCoverage(Justification = "Shouldn't be called, and as undefined can return or throw anything anyway")]
        object IEnumerator.Current => default!;

        void IEnumerator.Reset()
        {
            // Do nothing.
        }

        void IDisposable.Dispose()
        {
            // Do nothing.
        }

        public IPartition<TElement> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) => this;

        //public TElement? TryGetElementAt(int index, out bool found)
        //{
        //    found = false;
        //    return default;
        //}

        public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TElement element)
        {
            element = default;
            return false;
        }

        //public TElement? TryGetFirst(out bool found)
        //{
        //    found = false;
        //    return default;
        //}

        //public TElement? TryGetLast(out bool found)
        //{
        //    found = false;
        //    return default;
        //}

        public TElement[] ToArray() => Array.Empty<TElement>();

        public List<TElement> ToList() => new List<TElement>();

        public int GetCount(bool onlyIfCheap) => 0;
    }

    internal sealed class OrderedPartition<TElement> : IPartition<TElement>
    {
        private readonly OrderedEnumerable<TElement> _source;
        private readonly int _minIndexInclusive;
        private readonly int _maxIndexInclusive;
        private readonly bool _isMinIndexFromEnd;
        private readonly bool _isMaxIndexFromEnd;

        public OrderedPartition(OrderedEnumerable<TElement> source, int minIndexInclusive, int maxIndexInclusive, bool isMinIndexFromEnd = false, bool isMaxIndexFromEnd = false)
        {
            _source = source;
            _minIndexInclusive = minIndexInclusive;
            _maxIndexInclusive = maxIndexInclusive;
            _isMinIndexFromEnd = isMinIndexFromEnd;
            _isMaxIndexFromEnd = isMaxIndexFromEnd;
        }

        public IEnumerator<TElement> GetEnumerator() => _source.GetEnumerator(_minIndexInclusive, _maxIndexInclusive);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IPartition<TElement> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) => throw new NotImplementedException();

        public IPartition<TElement> Skip(int count)
        {
            int minIndex = unchecked(_minIndexInclusive + count);
            return unchecked((uint)minIndex > (uint)_maxIndexInclusive) ? EmptyPartition<TElement>.Instance : new OrderedPartition<TElement>(_source, minIndex, _maxIndexInclusive);
        }

        public IPartition<TElement> Take(int count)
        {
            int maxIndex = unchecked(_minIndexInclusive + count - 1);
            if (unchecked((uint)maxIndex >= (uint)_maxIndexInclusive))
            {
                return this;
            }

            return new OrderedPartition<TElement>(_source, _minIndexInclusive, maxIndex);
        }

        //public TElement? TryGetElementAt(int index, out bool found)
        //{
        //    if (unchecked((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive)))
        //    {
        //        return _source.TryGetElementAt(index + _minIndexInclusive, out found);
        //    }

        //    found = false;
        //    return default;
        //}

        public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TElement element) =>
            _source.TryGetElementAt(index, isIndexFromEnd, out element);

        //public TElement? TryGetFirst(out bool found) => _source.TryGetElementAt(_minIndexInclusive, out found);

        public TElement? TryGetLast(out bool found) =>
            _source.TryGetLast(_minIndexInclusive, _maxIndexInclusive, out found);

        public TElement[] ToArray() => _source.ToArray(_minIndexInclusive, _maxIndexInclusive);

        public List<TElement> ToList() => _source.ToList(_minIndexInclusive, _maxIndexInclusive);

        public int GetCount(bool onlyIfCheap) => _source.GetCount(_minIndexInclusive, _maxIndexInclusive, onlyIfCheap);
    }

    internal static class Partition
    {
        internal static (int NormalizedStartIndexInclusive, int NormalizedEndIndexExclusive, bool IsEmpty) Normalize(int count, int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd)
        {
            Debug.Assert(count >= 0);
            Debug.Assert(startIndexInclusive >= 0);
            Debug.Assert(endIndexExclusive >= 0);

            if (isStartIndexFromEnd)
            {
                startIndexInclusive = count - startIndexInclusive;
            }

            if (isEndIndexFromEnd)
            {
                endIndexExclusive = count - endIndexExclusive;
            }

            if (startIndexInclusive < 0)
            {
                startIndexInclusive = 0;
            }

            if (endIndexExclusive > count)
            {
                endIndexExclusive = count;
            }

            return (startIndexInclusive, endIndexExclusive, startIndexInclusive >= count || endIndexExclusive <= 0 || startIndexInclusive >= endIndexExclusive);
        }

        internal static (int NormalizedIndex, bool IsOutOfRange) Normalize(int count, int index, bool isIndexFromEnd)
        {
            Debug.Assert(count >= 0);
            Debug.Assert(index >= 0);

            if (isIndexFromEnd)
            {
                index = count - index;
                if (index < 0)
                {
                    return (0, true);
                }
            }

            return index < count ? (index, false) : (count, true);
        }

        internal static bool IsEmpty(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd)
        {
            Debug.Assert(startIndexInclusive >= 0);
            Debug.Assert(endIndexExclusive >= 0);

            return IsEmpty((uint)startIndexInclusive, (uint)endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd);
        }

        internal static bool IsEmpty(uint startIndexInclusive, uint endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
            isStartIndexFromEnd
                ? startIndexInclusive == 0 || (isEndIndexFromEnd && startIndexInclusive <= endIndexExclusive)
                : !isEndIndexFromEnd && (endIndexExclusive == 0 || startIndexInclusive >= endIndexExclusive);

        internal static IPartition<TSource> ToPartition<TSource>(this IList<TSource> source, int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd = false, bool isEndIndexFromEnd = false)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndexInclusive >= 0);
            Debug.Assert(endIndexExclusive >= 0);

            (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Normalize(source.Count, startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd);
            return isEmpty
                ? EmptyPartition<TSource>.Instance
                : new Enumerable.ListPartition<TSource>(source, normalizedStartIndexInclusive, normalizedEndIndexExclusive - 1);
        }

        internal static IPartition<TSource> ToPartition<TSource>(this IEnumerable<TSource> source, int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd = false, bool isEndIndexFromEnd = false)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndexInclusive >= 0);
            Debug.Assert(endIndexExclusive >= 0);

            return IsEmpty(startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd)
                ? EmptyPartition<TSource>.Instance
                : new Enumerable.EnumerablePartition<TSource>(source, startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd);
        }

        internal static IPartition<TSource> ToPartition<TSource>(this OrderedEnumerable<TSource> source, int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd = false, bool isEndIndexFromEnd = false)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndexInclusive >= 0);
            Debug.Assert(endIndexExclusive >= 0);

            return IsEmpty(startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd)
                ? EmptyPartition<TSource>.Instance
                : new OrderedPartition<TSource>(source, startIndexInclusive, endIndexExclusive - 1, isStartIndexFromEnd, isEndIndexFromEnd);
        }

        internal static IPartition<TElement> Take<TElement>(
            this IPartition<TElement> partition, int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd, Func<int, int, IPartition<TElement>> factory)
        {
            Debug.Assert(partition != null);
            Debug.Assert(startIndexInclusive >= 0);
            Debug.Assert(endIndexExclusive >= 0);
            Debug.Assert(factory != null);

            int count = partition.GetCount(onlyIfCheap: true);
            Debug.Assert(count >= 0);
            (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Normalize(count, startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd);

            if (isEmpty)
            {
                return EmptyPartition<TElement>.Instance;
            }

            if (startIndexInclusive == 0 && endIndexExclusive == count)
            {
                return partition;
            }

            return factory(normalizedStartIndexInclusive, normalizedEndIndexExclusive);
        }

        public static bool TryGetElementAt<TSource>(this IPartition<TSource> partition, int index, bool isIndexFromEnd, Func<int, TSource> factory, [MaybeNullWhen(false)] out TSource element)
        {
            element = default;
            if (index < 0)
            {
                return false;
            }

            int count = partition.GetCount(onlyIfCheap: true);
            Debug.Assert(count >= 0);
            if (isIndexFromEnd)
            {
                index = count - index;
                if (index < 0)
                {
                    return false;
                }
            }

            if (index >= count)
            {
                return false;
            }

            element = factory(index);
            return true;
        }
    }

    public static partial class Enumerable
    {
        /// <summary>
        /// An iterator that yields the items of part of an <see cref="IList{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        [DebuggerDisplay("Count = {Count}")]
        internal sealed class ListPartition<TSource> : Iterator<TSource>, IPartition<TSource>
        {
            private readonly IList<TSource> _source;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;

            public ListPartition(IList<TSource> source, int minIndexInclusive, int maxIndexInclusive, bool isMinIndexFromEnd = false, bool isMaxIndexFromEnd = false)
            {
                Debug.Assert(source != null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _minIndexInclusive = isMinIndexFromEnd ? source.Count - minIndexInclusive : minIndexInclusive;
                _maxIndexInclusive = isMaxIndexFromEnd ? source.Count - maxIndexInclusive : maxIndexInclusive;
            }

            public override Iterator<TSource> Clone() =>
                new ListPartition<TSource>(_source, _minIndexInclusive, _maxIndexInclusive);

            public override bool MoveNext()
            {
                // _state - 1 represents the zero-based index into the list.
                // Having a separate field for the index would be more readable. However, we save it
                // into _state with a bias to minimize field size of the iterator.
                int index = _state - 1;
                if (unchecked((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive))
                {
                    _current = _source[_minIndexInclusive + index];
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new SelectListPartitionIterator<TSource, TResult>(_source, selector, _minIndexInclusive, _maxIndexInclusive);

            public IPartition<TSource> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new ListPartition<TSource>(_source, _minIndexInclusive + normalizedStartIndexInclusive, _minIndexInclusive + normalizedEndIndexExclusive - 1));

            //public TSource? TryGetElementAt(int index, out bool found)
            //{
            //    if (unchecked((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive))
            //    {
            //        found = true;
            //        return _source[_minIndexInclusive + index];
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TSource element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _source[_minIndexInclusive + normalizedIndex], out element);

            //public TSource? TryGetFirst(out bool found)
            //{
            //    if (_source.Count > _minIndexInclusive)
            //    {
            //        found = true;
            //        return _source[_minIndexInclusive];
            //    }

            //    found = false;
            //    return default;
            //}

            //public TSource? TryGetLast(out bool found)
            //{
            //    int lastIndex = _source.Count - 1;
            //    if (lastIndex >= _minIndexInclusive)
            //    {
            //        found = true;
            //        return _source[Math.Min(lastIndex, _maxIndexInclusive)];
            //    }

            //    found = false;
            //    return default;
            //}

            private int Count
            {
                get
                {
                    int count = _source.Count;
                    if (count <= _minIndexInclusive)
                    {
                        return 0;
                    }

                    return Math.Min(count - 1, _maxIndexInclusive) - _minIndexInclusive + 1;
                }
            }

            public TSource[] ToArray()
            {
                int count = Count;
                if (count == 0)
                {
                    return Array.Empty<TSource>();
                }

                TSource[] array = new TSource[count];
                for (int i = 0, curIdx = _minIndexInclusive; i != array.Length; ++i, ++curIdx)
                {
                    array[i] = _source[curIdx];
                }

                return array;
            }

            public List<TSource> ToList()
            {
                int count = Count;
                if (count == 0)
                {
                    return new List<TSource>();
                }

                List<TSource> list = new List<TSource>(count);
                int end = _minIndexInclusive + count;
                for (int i = _minIndexInclusive; i != end; ++i)
                {
                    list.Add(_source[i]);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap) => Count;
        }

        /// <summary>
        /// An iterator that yields the items of part of an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        internal sealed class EnumerablePartition<TSource> : Iterator<TSource>, IPartition<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly int _startIndexInclusive;
            private readonly int _endIndexExclusive; // -1 if we want everything past _minIndexInclusive.
                                                     // If this is -1, it's impossible to set a limit on the count.

            private readonly bool _isStartIndexFromEnd;
            private readonly bool _isEndIndexFromEnd;
            private IEnumerator<TSource>? _enumerator;

            internal EnumerablePartition(IEnumerable<TSource> source, int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd = false, bool isEndIndexFromEnd = false)
            {
                Debug.Assert(source != null);
                Debug.Assert(!(source is IList<TSource>), $"The caller needs to check for {nameof(IList<TSource>)}.");
                Debug.Assert(startIndexInclusive >= 0);
                Debug.Assert(endIndexExclusive >= 0);

                // if (!isMinIndexFromEnd && !isMaxIndexFromEnd), then (minIndexInclusive < maxIndexExclusive).
                Debug.Assert(isStartIndexFromEnd || isEndIndexFromEnd || startIndexInclusive < endIndexExclusive);
                // if (isMinIndexFromEnd && isMaxIndexFromEnd), then (minIndexInclusive > maxIndexExclusive).
                Debug.Assert(!isStartIndexFromEnd || !isEndIndexFromEnd || startIndexInclusive > endIndexExclusive);

                _source = source;
                _startIndexInclusive = startIndexInclusive;
                _endIndexExclusive = endIndexExclusive;
                _isStartIndexFromEnd = isStartIndexFromEnd;
                _isEndIndexFromEnd = isEndIndexFromEnd;
            }

            // If this is true (e.g. at least one Take call was made), then we have an upper bound
            // on how many elements we can have.
            private bool IsNormalized => !_isStartIndexFromEnd && !_isEndIndexFromEnd;

            private int NormalizedCount => _endIndexExclusive - _startIndexInclusive; // This is that upper bound.

            public override Iterator<TSource> Clone() =>
                new EnumerablePartition<TSource>(_source, _startIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                if (_source.TryGetNonEnumeratedCount(out int sourceCount))
                {
                    (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Partition.Normalize(sourceCount, _startIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
                    return isEmpty ? 0 : normalizedEndIndexExclusive - normalizedStartIndexInclusive;
                }

                if (!IsNormalized)
                {
                    // If HasNormalizedLimit is false, we have to iterate the whole enumerable.
                    (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Partition.Normalize(_source.Count(), _startIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
                    return isEmpty ? 0 : normalizedEndIndexExclusive - normalizedStartIndexInclusive;
                }

                using IEnumerator<TSource> e = _source.GetEnumerator();
                int actualEndIndexExclusive = Skip(e, _endIndexExclusive);
                return Math.Max(actualEndIndexExclusive - _startIndexInclusive, 0);
            }

            public override bool MoveNext()
            {
                // Cases where GetEnumerator has not been called or Dispose has already
                // been called need to be handled explicitly, due to the default: clause.
                int taken = _state - 3;
                if (taken < -2)
                {
                    Dispose();
                    return false;
                }

                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        if (!SkipBeforeFirst(_enumerator))
                        {
                            // Reached the end before we finished skipping.
                            break;
                        }

                        _state = 3;
                        goto default;
                    default:
                        Debug.Assert(_enumerator != null);
                        if ((!IsNormalized || taken < NormalizedCount) && _enumerator.MoveNext())
                        {
                            if (IsNormalized)
                            {
                                // If we are taking an unknown number of elements, it's important not to increment _state.
                                // _state - 3 may eventually end up overflowing & we'll hit the Dispose branch even though
                                // we haven't finished enumerating.
                                _state++;
                            }
                            _current = _enumerator.Current;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector) =>
                new SelectIPartitionIterator<TSource, TResult>(this, selector);

            public IPartition<TSource> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd)
            {
                Debug.Assert(startIndexInclusive >= 0);
                Debug.Assert(endIndexExclusive >= 0);

                if (Partition.IsEmpty(startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd))
                {
                    return EmptyPartition<TSource>.Instance;
                }

                uint? mergedStartIndexInclusive = null;
                bool? isMergedStartIndexFromEnd = null; // TODO!!!
                if (isStartIndexFromEnd)
                {
                    if (_isStartIndexFromEnd && _isEndIndexFromEnd)
                    {
                        mergedStartIndexInclusive = Math.Min((uint)_startIndexInclusive, (uint)_endIndexExclusive + (uint)startIndexInclusive);
                        isMergedStartIndexFromEnd = true;
                    }
                    else if (!_isStartIndexFromEnd && !_isEndIndexFromEnd)
                    {
                        mergedStartIndexInclusive = (uint)Math.Max(_startIndexInclusive, Math.Max(0, _endIndexExclusive - startIndexInclusive));
                        isMergedStartIndexFromEnd = false;
                    }
                }
                else
                {
                    if (_isStartIndexFromEnd)
                    {
                        mergedStartIndexInclusive = (uint)Math.Max(0, _startIndexInclusive - startIndexInclusive);
                        isMergedStartIndexFromEnd = true;
                    }
                    else
                    {
                        mergedStartIndexInclusive = (uint)_startIndexInclusive + (uint)startIndexInclusive;
                        isMergedStartIndexFromEnd = false;
                    }
                }

                Debug.Assert(!isMergedStartIndexFromEnd.HasValue || isMergedStartIndexFromEnd == _isStartIndexFromEnd);

                uint? mergedEndIndexExclusive = null;
                bool? isMergedEndIndexFrmEnd = null; // TODO!!!
                if (isEndIndexFromEnd)
                {
                    if (_isEndIndexFromEnd)
                    {
                        mergedEndIndexExclusive = (uint)_endIndexExclusive + (uint)endIndexExclusive;
                        isMergedEndIndexFrmEnd = true;
                    }
                    else
                    {
                        mergedEndIndexExclusive = (uint)Math.Max(0, _endIndexExclusive - endIndexExclusive);
                        isMergedEndIndexFrmEnd = false;
                    }
                }
                else
                {
                    if (_isStartIndexFromEnd && _isEndIndexFromEnd)
                    {
                        mergedEndIndexExclusive = (uint)Math.Max(_endIndexExclusive, Math.Max(0, _startIndexInclusive - startIndexInclusive));
                        isMergedEndIndexFrmEnd = true;
                    }
                    else if (!_isStartIndexFromEnd && !_isEndIndexFromEnd)
                    {
                        mergedEndIndexExclusive = Math.Min((uint)_endIndexExclusive, (uint)_startIndexInclusive + (uint)startIndexInclusive);
                        isMergedEndIndexFrmEnd = false;
                    }
                }

                Debug.Assert(!isMergedEndIndexFrmEnd.HasValue || isMergedEndIndexFrmEnd == _isEndIndexFromEnd);

                if (mergedStartIndexInclusive.HasValue && mergedEndIndexExclusive.HasValue)
                {
                    uint startInclusive = mergedStartIndexInclusive.Value;
                    uint endExclusive = mergedEndIndexExclusive.Value;
                    if (startInclusive == _startIndexInclusive && endExclusive == _endIndexExclusive)
                    {
                        return this;
                    }

                    if (Partition.IsEmpty(startInclusive, endExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd))
                    {
                        return EmptyPartition<TSource>.Instance;
                    }

                    if (startInclusive <= int.MaxValue && endExclusive <= int.MaxValue)
                    {
                        return new EnumerablePartition<TSource>(_source, (int)startInclusive, (int)endExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
                    }
                }

                return new EnumerablePartition<TSource>(this, startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd);
            }

            //public IPartition<TSource> Skip(int count)
            //{
            //    int minIndexInclusive = unchecked(_startIndexInclusive + count);

            //    if (!HasNormalizedLimit)
            //    {
            //        if (minIndexInclusive < 0)
            //        {
            //            // If we don't know our max count and minIndex can no longer fit in a positive int,
            //            // then we will need to wrap ourselves in another iterator.
            //            // This can happen, for example, during e.Skip(int.MaxValue).Skip(int.MaxValue).
            //            return new EnumerablePartition<TSource>(this, count, 0, false, false);
            //        }
            //    }
            //    else if ((uint)minIndexInclusive >= (uint)_endIndexExclusive)
            //    {
            //        // If minIndex overflows and we have an upper bound, we will go down this branch.
            //        // We know our upper bound must be smaller than minIndex, since our upper bound fits in an int.
            //        // This branch should not be taken if we don't have a bound.
            //        return EmptyPartition<TSource>.Instance;
            //    }

            //    Debug.Assert(minIndexInclusive >= 0, $"We should have taken care of all cases when {nameof(minIndexInclusive)} overflows.");
            //    return new EnumerablePartition<TSource>(_source, minIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
            //}

            //public IPartition<TSource> Take(int count)
            //{
            //    int maxIndexExclusive = unchecked(_startIndexInclusive + count);
            //    if (!HasNormalizedLimit)
            //    {
            //        if (maxIndexExclusive <= 0)
            //        {
            //            // If we don't know our max count and maxIndex can no longer fit in a positive int,
            //            // then we will need to wrap ourselves in another iterator.
            //            // Note that although maxIndex may be too large, the difference between it and
            //            // _minIndexInclusive (which is count - 1) must fit in an int.
            //            // Example: e.Skip(50).Take(int.MaxValue).

            //            return new EnumerablePartition<TSource>(this, 0, count, false, false);
            //        }
            //    }
            //    else if (unchecked((uint)maxIndexExclusive >= (uint)_endIndexExclusive))
            //    {
            //        // If we don't know our max count, we can't go down this branch.
            //        // It's always possible for us to contain more than count items, as the rest
            //        // of the enumerable past _minIndexInclusive can be arbitrarily long.
            //        return this;
            //    }

            //    Debug.Assert(maxIndexExclusive > 0, $"We should have taken care of all cases when {nameof(maxIndexExclusive)} overflows.");
            //    return new EnumerablePartition<TSource>(_source, _startIndexInclusive, maxIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
            //}

            //public TSource? TryGetElementAt(int index, out bool found)
            //{
            //    // If the index is negative or >= our max count, return early.
            //    if (index >= 0 && (!IsNormalized || index < NormalizedCount))
            //    {
            //        using (IEnumerator<TSource> en = _source.GetEnumerator())
            //        {
            //            Debug.Assert(_startIndexInclusive + index >= 0, $"Adding {nameof(index)} caused {nameof(_startIndexInclusive)} to overflow.");

            //            if (SkipFromStartBefore(_startIndexInclusive + index, en) && en.MoveNext())
            //            {
            //                found = true;
            //                return en.Current;
            //            }
            //        }
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TSource element)
            {
                element = default;
                if (index < 0 || (index == 0 && isIndexFromEnd))
                {
                    return false;
                }

                if (_source.TryGetNonEnumeratedCount(out int sourceCount))
                {
                    (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Partition.Normalize(sourceCount, _startIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
                    if (isEmpty)
                    {
                        return false;
                    }

                    int count = normalizedEndIndexExclusive - normalizedStartIndexInclusive;
                    (int normalizedIndex, bool isOutOfRange) = Partition.Normalize(count, index, isIndexFromEnd);
                    return !isOutOfRange && TryGetElementFromStart(normalizedStartIndexInclusive + normalizedIndex, out element);
                }

                int mergedIndex;
                if (isIndexFromEnd)
                {
                    if (_isEndIndexFromEnd)
                    {
                        mergedIndex = _endIndexExclusive + index;
                        if (_isStartIndexFromEnd)
                        {
                            if (mergedIndex >= _startIndexInclusive)
                            {
                                return false;
                            }

                            return TryGetElementFromEnd(mergedIndex, out element);
                        }
                        else
                        {
                            return TryGetElementFromEnd(mergedIndex, out element, normalizedIndex => normalizedIndex >= _startIndexInclusive);
                        }
                    }
                    else
                    {
                        mergedIndex = _endIndexExclusive - index;
                        if (mergedIndex < 0)
                        {
                            return false;
                        }

                        if (_isStartIndexFromEnd)
                        {
                            return TryGetElementFromStart(mergedIndex, out element, e => !SkipBefore(_startIndexInclusive, e));
                        }
                        else
                        {
                            if (mergedIndex < _startIndexInclusive)
                            {
                                return false;
                            }

                            return TryGetElementFromStart(mergedIndex, out element);
                        }
                    }
                }
                else
                {
                    if (_isStartIndexFromEnd)
                    {
                        mergedIndex = _startIndexInclusive - index;
                        if (mergedIndex < 0)
                        {
                            return false;
                        }

                        if (_isEndIndexFromEnd)
                        {
                            if (mergedIndex < _endIndexExclusive)
                            {
                                return false;
                            }

                            return TryGetElementFromEnd(mergedIndex, out element);
                        }
                        else
                        {
                            return TryGetElementFromEnd(mergedIndex, out element, normalizedIndex => normalizedIndex < _endIndexExclusive);
                        }
                    }
                    else
                    {
                        mergedIndex = _startIndexInclusive + index;
                        if (_isEndIndexFromEnd)
                        {
                            return TryGetElementFromStart(mergedIndex, out element, e => SkipBefore(_endIndexExclusive, e));
                        }
                        else
                        {
                            if (mergedIndex >= _endIndexExclusive)
                            {
                                return false;
                            }

                            return TryGetElementFromStart(mergedIndex, out element);
                        }
                    }
                }
            }

            private bool TryGetElementFromStart(int index, [MaybeNullWhen(false)] out TSource element, Func<IEnumerator<TSource>, bool>? postCondition = null)
            {
                using IEnumerator<TSource> e = _source.GetEnumerator();
                if (SkipBefore(index, e) && e.MoveNext())
                {
                    element = e.Current;
                    if (postCondition is null || postCondition(e))
                    {
                        return true;
                    }
                }

                element = default;
                return false;
            }

            private bool TryGetElementFromEnd(int index, [MaybeNullWhen(false)] out TSource element, Func<int, bool>? postCondition = null)
            {
                using IEnumerator<TSource> e = _source.GetEnumerator();
                if (e.MoveNext())
                {
                    int currentIndex = 1;
                    Queue<TSource> queue = new();
                    queue.Enqueue(e.Current);
                    while (e.MoveNext())
                    {
                        checked
                        {
                            currentIndex++;
                        }

                        if (queue.Count == index)
                        {
                            queue.Dequeue();
                        }

                        queue.Enqueue(e.Current);
                    }

                    if (queue.Count == index && (postCondition is null || postCondition(checked(++currentIndex) - index)))
                    {
                        element = queue.Dequeue();
                        return true;
                    }
                }

                element = default;
                return false;
            }

            //public TSource? TryGetFirst(out bool found)
            //{
            //    using (IEnumerator<TSource> en = _source.GetEnumerator())
            //    {
            //        if (SkipBeforeFirst(en) && en.MoveNext())
            //        {
            //            found = true;
            //            return en.Current;
            //        }
            //    }

            //    found = false;
            //    return default;
            //}

            //public TSource? TryGetLast(out bool found)
            //{
            //    using (IEnumerator<TSource> en = _source.GetEnumerator())
            //    {
            //        if (SkipBeforeFirst(en) && en.MoveNext())
            //        {
            //            int remaining = Limit - 1; // Max number of items left, not counting the current element.
            //            int comparand = HasLimit ? 0 : int.MinValue; // If we don't have an upper bound, have the comparison always return true.
            //            TSource result;

            //            do
            //            {
            //                remaining--;
            //                result = en.Current;
            //            }
            //            while (remaining >= comparand && en.MoveNext());

            //            found = true;
            //            return result;
            //        }
            //    }

            //    found = false;
            //    return default;
            //}

            public void GetElements(Action<TSource> elementCallback)
            {
                if (_source.TryGetNonEnumeratedCount(out int sourceCount))
                {
                    (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Partition.Normalize(sourceCount, _startIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
                    if (!isEmpty)
                    {
                        using IEnumerator<TSource> e = _source.GetEnumerator();
                        int actualSkipCount = Skip(e, normalizedStartIndexInclusive);
                        Debug.Assert(actualSkipCount == normalizedStartIndexInclusive);
                        Take(e, normalizedEndIndexExclusive - normalizedStartIndexInclusive, elementCallback);
                        return;
                    }
                }

                if (_isStartIndexFromEnd)
                {
                    Queue<TSource> takeLastResult;
                    (takeLastResult, sourceCount) = TakeLast(_source, _startIndexInclusive);
                    (int normalizedStartIndexInclusive, int normalizedEndIndexExclusive, bool isEmpty) = Partition.Normalize(sourceCount, _startIndexInclusive, _endIndexExclusive, _isStartIndexFromEnd, _isEndIndexFromEnd);
                    if (!isEmpty)
                    {
                        using IEnumerator<TSource> e = takeLastResult.GetEnumerator();
                        Take(e, normalizedEndIndexExclusive - normalizedStartIndexInclusive, elementCallback);
                    }
                }
                else
                {
                    if (_isEndIndexFromEnd)
                    {
                        using IEnumerator<TSource> e = _source.GetEnumerator();
                        if (Skip(e, _startIndexInclusive) == _startIndexInclusive)
                        {
                            SkipLast(e, _endIndexExclusive, elementCallback);
                        }
                    }
                    else
                    {
                        using IEnumerator<TSource> e = _source.GetEnumerator();
                        if (Skip(e, _startIndexInclusive) == _startIndexInclusive)
                        {
                            Take(e, _endIndexExclusive - _startIndexInclusive, elementCallback);
                        }
                    }
                }
            }

            public TSource[] ToArray()
            {
                LargeArrayBuilder<TSource> arrayBuilder = new();
                GetElements(arrayBuilder.Add);
                return arrayBuilder.ToArray();
            }

            public List<TSource> ToList()
            {
                List<TSource> list = new();
                GetElements(list.Add);
                return list;
            }

            private bool SkipBeforeFirst(IEnumerator<TSource> e) => SkipBefore(_startIndexInclusive, e);

            private static bool SkipBefore(int indexExclusive, IEnumerator<TSource> en) => Skip(en, indexExclusive) == indexExclusive;

            private static int Skip(IEnumerator<TSource> e, int count)
            {
                Debug.Assert(count >= 0);
                return (int)Skip((uint)count, e);
            }

            private static uint Skip(uint count, IEnumerator<TSource> e)
            {
                Debug.Assert(e != null);

                for (uint index = 0; index < count; index++)
                {
                    if (!e.MoveNext())
                    {
                        return index;
                    }
                }

                return count;
            }

            private static void SkipLast(IEnumerator<TSource> e, int count, Action<TSource> elementCallback)
            {
                Debug.Assert(e != null);
                Debug.Assert(count > 0);

                Queue<TSource> queue = new();
                while (e.MoveNext())
                {
                    if (queue.Count == count)
                    {
                        do
                        {
                            elementCallback(queue.Dequeue());
                            queue.Enqueue(e.Current);
                        } while (e.MoveNext());

                        break;
                    }

                    queue.Enqueue(e.Current);
                }
            }

            private static void Take(IEnumerator<TSource> e, int count, Action<TSource> elementCallback)
            {
                Debug.Assert(e != null);

                for (int index = 0; index < count; index++)
                {
                    bool shouldMoveNext = e.MoveNext();
                    Debug.Assert(shouldMoveNext);
                    elementCallback(e.Current);
                }
            }

            private static (Queue<TSource> Result, int TotalCount) TakeLast(IEnumerable<TSource> source, int count)
            {
                Debug.Assert(source != null);
                Debug.Assert(count > 0);

                int totalCount = 0;
                Queue<TSource> queue = new();
                using IEnumerator<TSource> e = source.GetEnumerator();
                while (e.MoveNext())
                {
                    checked
                    {
                        totalCount++;
                    }

                    queue.Enqueue(e.Current);
                    if (queue.Count > count)
                    {
                        queue.Dequeue();
                    }
                }

                return (queue, totalCount);
            }
        }
    }
}
