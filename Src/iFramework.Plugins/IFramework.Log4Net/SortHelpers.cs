using System;
using System.Collections;
using System.Collections.Generic;

namespace IFramework.Log4Net
{
    /// <summary>
    ///     Provides helpers to sort log events and associated continuations.
    /// </summary>
    internal static class SortHelpers
    {
        /// <summary>
        ///     Performs bucket sort (group by) on an array of items and returns a dictionary for easy traversal of the result set.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="inputs">The inputs.</param>
        /// <param name="keySelector">The key selector function.</param>
        /// <returns>
        ///     Dictionary where keys are unique input keys, and values are lists of .
        /// </returns>
        public static Dictionary<TKey, List<TValue>> BucketSort<TValue, TKey>(this IEnumerable<TValue> inputs, KeySelector<TValue, TKey> keySelector)
        {
            var buckets = new Dictionary<TKey, List<TValue>>();

            foreach (var input in inputs)
            {
                var keyValue = keySelector(input);
                List<TValue> eventsInBucket;
                if (!buckets.TryGetValue(keyValue, out eventsInBucket))
                {
                    eventsInBucket = new List<TValue>();
                    buckets.Add(keyValue, eventsInBucket);
                }

                eventsInBucket.Add(input);
            }

            return buckets;
        }

        /// <summary>
        ///     Performs bucket sort (group by) on an array of items and returns a dictionary for easy traversal of the result set.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="inputs">The inputs.</param>
        /// <param name="keySelector">The key selector function.</param>
        /// <returns>
        ///     Dictionary where keys are unique input keys, and values are lists of .
        /// </returns>
        public static ReadOnlySingleBucketDictionary<TKey, IList<TValue>> BucketSort<TValue, TKey>(this IList<TValue> inputs, KeySelector<TValue, TKey> keySelector)
        {
            return BucketSort(inputs, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        ///     Performs bucket sort (group by) on an array of items and returns a dictionary for easy traversal of the result set.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="inputs">The inputs.</param>
        /// <param name="keySelector">The key selector function.</param>
        /// <param name="keyComparer">The key comparer function.</param>
        /// <returns>
        ///     Dictionary where keys are unique input keys, and values are lists of.
        /// </returns>
        public static ReadOnlySingleBucketDictionary<TKey, IList<TValue>> BucketSort<TValue, TKey>(this IList<TValue> inputs, KeySelector<TValue, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
        {
            Dictionary<TKey, IList<TValue>> buckets = null;
            var singleBucketFirstKey = false;
            var singleBucketKey = default(TKey);
            for (var i = 0; i < inputs.Count; i++)
            {
                var keyValue = keySelector(inputs[i]);
                if (!singleBucketFirstKey)
                {
                    singleBucketFirstKey = true;
                    singleBucketKey = keyValue;
                }
                else if (buckets == null)
                {
                    if (!keyComparer.Equals(singleBucketKey, keyValue))
                    {
                        // Multiple buckets needed, allocate full dictionary
                        buckets = new Dictionary<TKey, IList<TValue>>(keyComparer);
                        var bucket = new List<TValue>(i);
                        for (var j = 0; j < i; j++)
                        {
                            bucket.Add(inputs[j]);
                        }
                        buckets[singleBucketKey] = bucket;
                        bucket = new List<TValue>();
                        bucket.Add(inputs[i]);
                        buckets[keyValue] = bucket;
                    }
                }
                else
                {
                    IList<TValue> eventsInBucket;
                    if (!buckets.TryGetValue(keyValue, out eventsInBucket))
                    {
                        eventsInBucket = new List<TValue>();
                        buckets.Add(keyValue, eventsInBucket);
                    }
                    eventsInBucket.Add(inputs[i]);
                }
            }

            if (buckets != null)
            {
                return new ReadOnlySingleBucketDictionary<TKey, IList<TValue>>(buckets, keyComparer);
            }
            return new ReadOnlySingleBucketDictionary<TKey, IList<TValue>>(new KeyValuePair<TKey, IList<TValue>>(singleBucketKey, inputs), keyComparer);
        }

        /// <summary>
        ///     Key selector delegate.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="value">Value to extract key information from.</param>
        /// <returns>Key selected from log event.</returns>
        internal delegate TKey KeySelector<in TValue, out TKey>(TValue value);

        /// <summary>
        ///     Single-Bucket optimized readonly dictionary. Uses normal internally Dictionary if multiple buckets are needed.
        ///     Avoids allocating a new dictionary, when all items are using the same bucket
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        public struct ReadOnlySingleBucketDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        {
            private readonly KeyValuePair<TKey, TValue>? _singleBucket;
            private readonly Dictionary<TKey, TValue> _multiBucket;
            public IEqualityComparer<TKey> Comparer { get; }

            public ReadOnlySingleBucketDictionary(KeyValuePair<TKey, TValue> singleBucket)
                : this(singleBucket, EqualityComparer<TKey>.Default) { }

            public ReadOnlySingleBucketDictionary(Dictionary<TKey, TValue> multiBucket)
                : this(multiBucket, EqualityComparer<TKey>.Default) { }

            public ReadOnlySingleBucketDictionary(KeyValuePair<TKey, TValue> singleBucket, IEqualityComparer<TKey> comparer)
            {
                Comparer = comparer;
                _multiBucket = null;
                _singleBucket = singleBucket;
            }

            public ReadOnlySingleBucketDictionary(Dictionary<TKey, TValue> multiBucket, IEqualityComparer<TKey> comparer)
            {
                Comparer = comparer;
                _multiBucket = multiBucket;
                _singleBucket = default(KeyValuePair<TKey, TValue>);
            }

            /// <inheritDoc />
            public int Count
            {
                get
                {
                    if (_multiBucket != null)
                    {
                        return _multiBucket.Count;
                    }
                    if (_singleBucket.HasValue)
                    {
                        return 1;
                    }
                    return 0;
                }
            }

            /// <inheritDoc />
            public ICollection<TKey> Keys
            {
                get
                {
                    if (_multiBucket != null)
                    {
                        return _multiBucket.Keys;
                    }
                    if (_singleBucket.HasValue)
                    {
                        return new[] {_singleBucket.Value.Key};
                    }
                    return ArrayHelper.Empty<TKey>();
                }
            }

            /// <inheritDoc />
            public ICollection<TValue> Values
            {
                get
                {
                    if (_multiBucket != null)
                    {
                        return _multiBucket.Values;
                    }
                    if (_singleBucket.HasValue)
                    {
                        return new[] {_singleBucket.Value.Value};
                    }
                    return ArrayHelper.Empty<TValue>();
                }
            }

            /// <inheritDoc />
            public bool IsReadOnly => true;

            /// <summary>
            ///     Allows direct lookup of existing keys. If trying to access non-existing key exception is thrown.
            ///     Consider to use <see cref="TryGetValue(TKey, out TValue)" /> instead for better safety.
            /// </summary>
            /// <param name="key">Key value for lookup</param>
            /// <returns>Mapped value found</returns>
            public TValue this[TKey key]
            {
                get
                {
                    if (_multiBucket != null)
                    {
                        return _multiBucket[key];
                    }
                    if (_singleBucket.HasValue && Comparer.Equals(_singleBucket.Value.Key, key))
                    {
                        return _singleBucket.Value.Value;
                    }
                    throw new KeyNotFoundException();
                }
                set => throw new NotSupportedException("Readonly");
            }

            /// <summary>
            ///     Non-Allocating struct-enumerator
            /// </summary>
            public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
            {
                private bool _singleBucketFirstRead;
                private readonly KeyValuePair<TKey, TValue> _singleBucket;
                private readonly IEnumerator<KeyValuePair<TKey, TValue>> _multiBuckets;

                internal Enumerator(Dictionary<TKey, TValue> multiBucket)
                {
                    _singleBucketFirstRead = false;
                    _singleBucket = default(KeyValuePair<TKey, TValue>);
                    _multiBuckets = multiBucket.GetEnumerator();
                }

                internal Enumerator(KeyValuePair<TKey, TValue> singleBucket)
                {
                    _singleBucketFirstRead = false;
                    _singleBucket = singleBucket;
                    _multiBuckets = null;
                }

                public KeyValuePair<TKey, TValue> Current
                {
                    get
                    {
                        if (_multiBuckets != null)
                        {
                            return new KeyValuePair<TKey, TValue>(_multiBuckets.Current.Key, _multiBuckets.Current.Value);
                        }
                        return new KeyValuePair<TKey, TValue>(_singleBucket.Key, _singleBucket.Value);
                    }
                }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    if (_multiBuckets != null)
                    {
                        _multiBuckets.Dispose();
                    }
                }

                public bool MoveNext()
                {
                    if (_multiBuckets != null)
                    {
                        return _multiBuckets.MoveNext();
                    }
                    if (_singleBucketFirstRead)
                    {
                        return false;
                    }
                    return _singleBucketFirstRead = true;
                }

                public void Reset()
                {
                    if (_multiBuckets != null)
                    {
                        _multiBuckets.Reset();
                    }
                    else
                    {
                        _singleBucketFirstRead = false;
                    }
                }
            }

            public Enumerator GetEnumerator()
            {
                if (_multiBucket != null)
                {
                    return new Enumerator(_multiBucket);
                }
                if (_singleBucket.HasValue)
                {
                    return new Enumerator(_singleBucket.Value);
                }
                return new Enumerator(new Dictionary<TKey, TValue>());
            }

            /// <inheritDoc />
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <inheritDoc />
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <inheritDoc />
            public bool ContainsKey(TKey key)
            {
                if (_multiBucket != null)
                {
                    return _multiBucket.ContainsKey(key);
                }
                if (_singleBucket.HasValue)
                {
                    return Comparer.Equals(_singleBucket.Value.Key, key);
                }
                return false;
            }

            /// <summary>Will always throw, as dictionary is readonly</summary>
            public void Add(TKey key, TValue value)
            {
                throw new NotSupportedException(); // Readonly
            }

            /// <summary>Will always throw, as dictionary is readonly</summary>
            public bool Remove(TKey key)
            {
                throw new NotSupportedException(); // Readonly
            }

            /// <inheritDoc />
            public bool TryGetValue(TKey key, out TValue value)
            {
                if (_multiBucket != null)
                {
                    return _multiBucket.TryGetValue(key, out value);
                }
                if (_singleBucket.HasValue && Comparer.Equals(_singleBucket.Value.Key, key))
                {
                    value = _singleBucket.Value.Value;
                    return true;
                }
                value = default(TValue);
                return false;
            }

            /// <summary>Will always throw, as dictionary is readonly</summary>
            public void Add(KeyValuePair<TKey, TValue> item)
            {
                throw new NotSupportedException(); // Readonly
            }

            /// <summary>Will always throw, as dictionary is readonly</summary>
            public void Clear()
            {
                throw new NotSupportedException(); // Readonly
            }

            /// <inheritDoc />
            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                if (_multiBucket != null)
                {
                    return ((IDictionary<TKey, TValue>) _multiBucket).Contains(item);
                }
                if (_singleBucket.HasValue)
                {
                    return Comparer.Equals(_singleBucket.Value.Key, item.Key) && EqualityComparer<TValue>.Default.Equals(_singleBucket.Value.Value, item.Value);
                }
                return false;
            }

            /// <inheritDoc />
            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                if (_multiBucket != null)
                {
                    ((IDictionary<TKey, TValue>) _multiBucket).CopyTo(array, arrayIndex);
                }
                else if (_singleBucket.HasValue)
                {
                    array[arrayIndex] = _singleBucket.Value;
                }
            }

            /// <summary>Will always throw, as dictionary is readonly</summary>
            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                throw new NotSupportedException(); // Readonly
            }
        }
    }
}