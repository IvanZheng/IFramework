using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace IFramework.Infrastructure
{
    public class ConcurrentLimitedSizeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dict;
        private readonly int _size;
        private ConcurrentQueue<TKey> _queue;

        public ConcurrentLimitedSizeDictionary(int size)
        {
            _size = size;
            _dict = new ConcurrentDictionary<TKey, TValue>(16, size + 1);
            _queue = new ConcurrentQueue<TKey>();
        }

        public void Add(TKey key, TValue value)
        {
            lock (this)
            {
                if (_dict.TryAdd(key, value))
                {
                    if (_queue.Count == _size)
                    {
                        if (_queue.TryDequeue(out var keyIndex))
                        {
                            _dict.TryRemove(keyIndex);
                        }
                    }

                    _queue.Enqueue(key);
                }
            }
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            lock (this)
            {
                if (_dict.TryRemove(key))
                {
                    ConcurrentQueue<TKey> newQueue = new ConcurrentQueue<TKey>();
                    foreach (TKey item in _queue)
                    {
                        if (!item.Equals(key))
                        {
                            newQueue.Enqueue(item);
                        }
                    }

                    _queue = newQueue;
                    return true;
                }

                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _dict[key];
            set
            {
                lock (this)
                {
                    if (_dict.ContainsKey(key))
                    {
                        _dict[key] = value;
                    }
                    else
                    {
                        Add(key, value);
                    }
                }
                
            } 
        }

        public ICollection<TKey> Keys => _dict.Keys;
        public ICollection<TValue> Values => _dict.Values;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            lock (this)
            {
                _dict.Clear();
                _queue = new ConcurrentQueue<TKey>();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            for (int i = 0; i < _dict.Count - arrayIndex; i++)
            {
                var key = _dict.Keys.ToArray()[i + arrayIndex];
                array[i] = new KeyValuePair<TKey, TValue>(key, _dict[key]);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count => _dict.Count;
        public bool IsReadOnly => false;
    }
}