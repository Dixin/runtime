// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        static partial void CreateSelectIPartitionIterator<TResult, TSource>(
            Func<TSource, TResult> selector, IPartition<TSource> partition, ref IEnumerable<TResult>? result)
        {
            result = partition is EmptyPartition<TSource> ?
                EmptyPartition<TResult>.Instance :
                new SelectIPartitionIterator<TSource, TResult>(partition, selector);
        }

        private sealed partial class SelectEnumerableIterator<TSource, TResult> : IIListProvider<TResult>
        {
            public TResult[] ToArray()
            {
                var builder = new LargeArrayBuilder<TResult>(initialize: true);

                foreach (TSource item in _source)
                {
                    builder.Add(_selector(item));
                }

                return builder.ToArray();
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (TSource item in _source)
                {
                    list.Add(_selector(item));
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource item in _source)
                {
                    _selector(item);
                    checked
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private sealed partial class SelectArrayIterator<TSource, TResult> : IPartition<TResult>
        {
            public TResult[] ToArray()
            {
                // See assert in constructor.
                // Since _source should never be empty, we don't check for 0/return Array.Empty.
                Debug.Assert(_source.Length > 0);

                var results = new TResult[_source.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                TSource[] source = _source;
                var results = new List<TResult>(source.Length);
                for (int i = 0; i < source.Length; i++)
                {
                    results.Add(_selector(source[i]));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                if (!onlyIfCheap)
                {
                    foreach (TSource item in _source)
                    {
                        _selector(item);
                    }
                }

                return _source.Length;
            }

            public IPartition<TResult> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new SelectListPartitionIterator<TSource, TResult>(_source, _selector, normalizedStartIndexInclusive, normalizedEndIndexExclusive - 1));

            //public TResult? TryGetElementAt(int index, out bool found)
            //{
            //    if (unchecked((uint)index < (uint)_source.Length))
            //    {
            //        found = true;
            //        return _selector(_source[index]);
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TResult element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _selector(_source[normalizedIndex]), out element);

            //public TResult TryGetFirst(out bool found)
            //{
            //    Debug.Assert(_source.Length > 0); // See assert in constructor

            //    found = true;
            //    return _selector(_source[0]);
            //}

            //public TResult TryGetLast(out bool found)
            //{
            //    Debug.Assert(_source.Length > 0); // See assert in constructor

            //    found = true;
            //    return _selector(_source[_source.Length - 1]);
            //}
        }

        private sealed partial class SelectRangeIterator<TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly int _startInclusive;
            private readonly int _endExclusive;
            private readonly Func<int, TResult> _selector;

            public SelectRangeIterator(int startInclusive, int endExclusive, Func<int, TResult> selector)
            {
                Debug.Assert(startInclusive < endExclusive);
                Debug.Assert((uint)(endExclusive - startInclusive) <= (uint)int.MaxValue);
                Debug.Assert(selector != null);

                _startInclusive = startInclusive;
                _endExclusive = endExclusive;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new SelectRangeIterator<TResult>(_startInclusive, _endExclusive, _selector);

            public override bool MoveNext()
            {
                if (_state < 1 || _state == (_endExclusive - _startInclusive + 1))
                {
                    Dispose();
                    return false;
                }

                int index = _state++ - 1;
                Debug.Assert(_startInclusive < _endExclusive - index);
                _current = _selector(_startInclusive + index);
                return true;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectRangeIterator<TResult2>(_startInclusive, _endExclusive, CombineSelectors(_selector, selector));

            public TResult[] ToArray()
            {
                var results = new TResult[_endExclusive - _startInclusive];
                int srcIndex = _startInclusive;
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(srcIndex++);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                var results = new List<TResult>(_endExclusive - _startInclusive);
                for (int i = _startInclusive; i != _endExclusive; i++)
                {
                    results.Add(_selector(i));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of the selector,
                // run it provided `onlyIfCheap` is false.
                if (!onlyIfCheap)
                {
                    for (int i = _startInclusive; i != _endExclusive; i++)
                    {
                        _selector(i);
                    }
                }

                return _endExclusive - _startInclusive;
            }

            public IPartition<TResult> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new SelectRangeIterator<TResult>(_startInclusive + normalizedStartIndexInclusive, _startInclusive + normalizedEndIndexExclusive, _selector));

            //public TResult? TryGetElementAt(int index, out bool found)
            //{
            //    if ((uint)index < (uint)(_endExclusive - _startInclusive))
            //    {
            //        found = true;
            //        return _selector(_startInclusive + index);
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TResult element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _selector(_startInclusive + normalizedIndex), out element);

            //public TResult TryGetFirst(out bool found)
            //{
            //    Debug.Assert(_endExclusive > _startInclusive);
            //    found = true;
            //    return _selector(_startInclusive);
            //}

            //public TResult TryGetLast(out bool found)
            //{
            //    Debug.Assert(_endExclusive > _startInclusive);
            //    found = true;
            //    return _selector(_endExclusive - 1);
            //}
        }

        private sealed partial class SelectListIterator<TSource, TResult> : IPartition<TResult>
        {
            public TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                int count = _source.Count;
                var results = new List<TResult>(count);
                for (int i = 0; i < count; i++)
                {
                    results.Add(_selector(_source[i]));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                int count = _source.Count;

                if (!onlyIfCheap)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _selector(_source[i]);
                    }
                }

                return count;
            }

            public IPartition<TResult> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new SelectListPartitionIterator<TSource, TResult>(_source, _selector, normalizedStartIndexInclusive, normalizedEndIndexExclusive - 1));

            //public TResult? TryGetElementAt(int index, out bool found)
            //{
            //    if (unchecked((uint)index < (uint)_source.Count))
            //    {
            //        found = true;
            //        return _selector(_source[index]);
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TResult element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _selector(_source[normalizedIndex]), out element);

            //public TResult? TryGetFirst(out bool found)
            //{
            //    if (_source.Count != 0)
            //    {
            //        found = true;
            //        return _selector(_source[0]);
            //    }

            //    found = false;
            //    return default;
            //}

            //public TResult? TryGetLast(out bool found)
            //{
            //    int len = _source.Count;
            //    if (len != 0)
            //    {
            //        found = true;
            //        return _selector(_source[len - 1]);
            //    }

            //    found = false;
            //    return default;
            //}
        }

        private sealed partial class SelectIListIterator<TSource, TResult> : IPartition<TResult>
        {
            public TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                int count = _source.Count;
                var results = new List<TResult>(count);
                for (int i = 0; i < count; i++)
                {
                    results.Add(_selector(_source[i]));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                int count = _source.Count;

                if (!onlyIfCheap)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _selector(_source[i]);
                    }
                }

                return count;
            }

            public IPartition<TResult> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new SelectListPartitionIterator<TSource, TResult>(_source, _selector, normalizedStartIndexInclusive, normalizedEndIndexExclusive - 1));

            //public TResult? TryGetElementAt(int index, out bool found)
            //{
            //    if (unchecked((uint)index < (uint)_source.Count))
            //    {
            //        found = true;
            //        return _selector(_source[index]);
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen((false))] out TResult element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _selector(_source[normalizedIndex]), out element);

            //public TResult? TryGetFirst(out bool found)
            //{
            //    if (_source.Count != 0)
            //    {
            //        found = true;
            //        return _selector(_source[0]);
            //    }

            //    found = false;
            //    return default;
            //}

            //public TResult? TryGetLast(out bool found)
            //{
            //    int len = _source.Count;
            //    if (len != 0)
            //    {
            //        found = true;
            //        return _selector(_source[len - 1]);
            //    }

            //    found = false;
            //    return default;
            //}
        }

        /// <summary>
        /// An iterator that maps each item of an <see cref="IPartition{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source partition.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed class SelectIPartitionIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly IPartition<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public SelectIPartitionIterator(IPartition<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new SelectIPartitionIterator<TSource, TResult>(_source, _selector);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator != null);
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectIPartitionIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));

            public IPartition<TResult> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) => new
                SelectIPartitionIterator<TSource, TResult>(_source.Take(startIndexInclusive, endIndexExclusive, isStartIndexFromEnd, isEndIndexFromEnd), _selector);

            //public IPartition<TResult> Skip(int count)
            //{
            //    Debug.Assert(count > 0);
            //    return new SelectIPartitionIterator<TSource, TResult>(_source.Skip(count), _selector);
            //}

            //public IPartition<TResult> Take(int count)
            //{
            //    Debug.Assert(count > 0);
            //    return new SelectIPartitionIterator<TSource, TResult>(_source.Take(count), _selector);
            //}

            //public TResult? TryGetElementAt(int index, out bool found)
            //{
            //    bool sourceFound;
            //    TSource? input = _source.TryGetElementAt(index, out sourceFound);
            //    found = sourceFound;
            //    return sourceFound ? _selector(input!) : default!;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TResult element)
            {
                if (_source.TryGetElementAt(index, isIndexFromEnd, out TSource? input))
                {
                    element = _selector(input);
                    return true;
                }

                element = default;
                return false;
            }

            public TResult? TryGetFirst(out bool found)
            {
                bool sourceFound;
                TSource? input = _source.TryGetFirst(out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input!) : default!;
            }

            public TResult? TryGetLast(out bool found)
            {
                bool sourceFound;
                TSource? input = _source.TryGetLast(out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input!) : default!;
            }

            private TResult[] LazyToArray()
            {
                Debug.Assert(_source.GetCount(onlyIfCheap: true) == -1);

                var builder = new LargeArrayBuilder<TResult>(initialize: true);
                foreach (TSource input in _source)
                {
                    builder.Add(_selector(input));
                }
                return builder.ToArray();
            }

            private TResult[] PreallocatingToArray(int count)
            {
                Debug.Assert(count > 0);
                Debug.Assert(count == _source.GetCount(onlyIfCheap: true));

                TResult[] array = new TResult[count];
                int index = 0;
                foreach (TSource input in _source)
                {
                    array[index] = _selector(input);
                    ++index;
                }

                return array;
            }

            public TResult[] ToArray()
            {
                int count = _source.GetCount(onlyIfCheap: true);
                return count switch
                {
                    -1 => LazyToArray(),
                    0 => Array.Empty<TResult>(),
                    _ => PreallocatingToArray(count),
                };
            }

            public List<TResult> ToList()
            {
                int count = _source.GetCount(onlyIfCheap: true);
                List<TResult> list;
                switch (count)
                {
                    case -1:
                        list = new List<TResult>();
                        break;
                    case 0:
                        return new List<TResult>();
                    default:
                        list = new List<TResult>(count);
                        break;
                }

                foreach (TSource input in _source)
                {
                    list.Add(_selector(input));
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (!onlyIfCheap)
                {
                    // In case someone uses Count() to force evaluation of
                    // the selector, run it provided `onlyIfCheap` is false.

                    int count = 0;

                    foreach (TSource item in _source)
                    {
                        _selector(item);
                        checked { count++; }
                    }

                    return count;
                }

                return _source.GetCount(onlyIfCheap);
            }
        }

        /// <summary>
        /// An iterator that maps each item of part of an <see cref="IList{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {Count}")]
        private sealed class SelectListPartitionIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;

            public SelectListPartitionIterator(IList<TSource> source, Func<TSource, TResult> selector, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _selector = selector;
                _minIndexInclusive = minIndexInclusive;
                _maxIndexInclusive = maxIndexInclusive;
            }

            public override Iterator<TResult> Clone() =>
                new SelectListPartitionIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, _maxIndexInclusive);

            public override bool MoveNext()
            {
                // _state - 1 represents the zero-based index into the list.
                // Having a separate field for the index would be more readable. However, we save it
                // into _state with a bias to minimize field size of the iterator.
                int index = _state - 1;
                if (unchecked((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive))
                {
                    _current = _selector(_source[_minIndexInclusive + index]);
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectListPartitionIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector), _minIndexInclusive, _maxIndexInclusive);

            public IPartition<TResult> Take(int startIndexInclusive, int endIndexExclusive, bool isStartIndexFromEnd, bool isEndIndexFromEnd) =>
                this.Take(
                    startIndexInclusive,
                    endIndexExclusive,
                    isStartIndexFromEnd,
                    isEndIndexFromEnd,
                    (normalizedStartIndexInclusive, normalizedEndIndexExclusive) => new SelectListPartitionIterator<TSource, TResult>(_source, _selector, _minIndexInclusive + normalizedStartIndexInclusive, _minIndexInclusive + normalizedEndIndexExclusive - 1));

            //public TResult? TryGetElementAt(int index, out bool found)
            //{
            //    if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
            //    {
            //        found = true;
            //        return _selector(_source[_minIndexInclusive + index]);
            //    }

            //    found = false;
            //    return default;
            //}

            public bool TryGetElementAt(int index, bool isIndexFromEnd, [MaybeNullWhen(false)] out TResult element) =>
                this.TryGetElementAt(index, isIndexFromEnd, normalizedIndex => _selector(_source[_minIndexInclusive + normalizedIndex]), out element);

            //public TResult? TryGetFirst(out bool found)
            //{
            //    if (_source.Count > _minIndexInclusive)
            //    {
            //        found = true;
            //        return _selector(_source[_minIndexInclusive]);
            //    }

            //    found = false;
            //    return default;
            //}

            //public TResult? TryGetLast(out bool found)
            //{
            //    int lastIndex = _source.Count - 1;
            //    if (lastIndex >= _minIndexInclusive)
            //    {
            //        found = true;
            //        return _selector(_source[Math.Min(lastIndex, _maxIndexInclusive)]);
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

            public TResult[] ToArray()
            {
                int count = Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                TResult[] array = new TResult[count];
                for (int i = 0, curIdx = _minIndexInclusive; i != array.Length; ++i, ++curIdx)
                {
                    array[i] = _selector(_source[curIdx]);
                }

                return array;
            }

            public List<TResult> ToList()
            {
                int count = Count;
                if (count == 0)
                {
                    return new List<TResult>();
                }

                List<TResult> list = new List<TResult>(count);
                int end = _minIndexInclusive + count;
                for (int i = _minIndexInclusive; i != end; ++i)
                {
                    list.Add(_selector(_source[i]));
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                int count = Count;

                if (!onlyIfCheap)
                {
                    int end = _minIndexInclusive + count;
                    for (int i = _minIndexInclusive; i != end; ++i)
                    {
                        _selector(_source[i]);
                    }
                }

                return count;
            }
        }
    }
}
