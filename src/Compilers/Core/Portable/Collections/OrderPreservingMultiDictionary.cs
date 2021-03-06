﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// A MultiDictionary that allows only adding, and 
    /// preserves the order of values added to the dictionary.
    /// Thread-safe for reading, but not for adding.
    /// </summary>
    /// <remarks>
    /// Always uses the default comparer.
    /// </remarks>
    internal sealed class OrderPreservingMultiDictionary<K, V>
    {
        #region Pooling

        private readonly ObjectPool<OrderPreservingMultiDictionary<K, V>> pool;

        private OrderPreservingMultiDictionary(ObjectPool<OrderPreservingMultiDictionary<K, V>> pool)
        {
            this.pool = pool;
        }

        public void Free()
        {
            if (this.dictionary != null)
            {
                this.dictionary.Free();
                this.dictionary = null;
            }

            if (this.pool != null)
            {
                this.pool.Free(this);
            }
        }

        // global pool
        private static readonly ObjectPool<OrderPreservingMultiDictionary<K, V>> PoolInstance = CreatePool();

        // if someone needs to create a pool;
        public static ObjectPool<OrderPreservingMultiDictionary<K, V>> CreatePool()
        {
            ObjectPool<OrderPreservingMultiDictionary<K, V>> pool = null;
            pool = new ObjectPool<OrderPreservingMultiDictionary<K, V>>(() => new OrderPreservingMultiDictionary<K, V>(pool), 16); // Size is a guess.
            return pool;
        }

        public static OrderPreservingMultiDictionary<K, V> GetInstance()
        {
            var instance = PoolInstance.Allocate();
            Debug.Assert(instance.IsEmpty);
            return instance;
        }

        #endregion Pooling

        public OrderPreservingMultiDictionary()
        {
        }

        // store either a single V or an ArrayBuilder<V>
        /// <summary>
        /// Each value is either a single V or an <see cref="ArrayBuilder{V}"/>.
        /// Null when the dictionary is empty.
        /// Don't access the field directly.
        /// </summary>
        private PooledDictionary<K, object> dictionary;

        private void EnsureDictionary()
        {
            this.dictionary = this.dictionary ?? PooledDictionary<K, object>.GetInstance();
        }

        public bool IsEmpty
        {
            get { return this.dictionary == null; }
        }

        /// <summary>
        /// Add a value to the dictionary.
        /// </summary>
        public void Add(K k, V v)
        {
            object item;
            if (!this.IsEmpty && this.dictionary.TryGetValue(k, out item))
            {
                var arrayBuilder = item as ArrayBuilder<V>;
                if (arrayBuilder == null)
                {
                    // Promote from singleton V to ArrayBuilder<V>.
                    Debug.Assert(item is V, "Item must be either a V or an ArrayBuilder<V>");
                    arrayBuilder = new ArrayBuilder<V>(2);
                    arrayBuilder.Add((V)item);
                    arrayBuilder.Add(v);
                    this.dictionary[k] = arrayBuilder;
                }
                else
                {
                    arrayBuilder.Add(v);
                }
            }
            else
            {
                this.EnsureDictionary();
                this.dictionary[k] = v;
            }
        }

        /// <summary>
        /// Add multiple values to the dictionary.
        /// </summary>
        public void AddRange(K k, ImmutableArray<V> values)
        {
            if (values.IsEmpty)
                return;

            object item;
            ArrayBuilder<V> arrayBuilder;

            if (!this.IsEmpty && this.dictionary.TryGetValue(k, out item))
            {
                arrayBuilder = item as ArrayBuilder<V>;
                if (arrayBuilder == null)
                {
                    // Promote from singleton V to ArrayBuilder<V>.
                    Debug.Assert(item is V, "Item must be either a V or an ArrayBuilder<V>");
                    arrayBuilder = new ArrayBuilder<V>(1 + values.Length);
                    arrayBuilder.Add((V)item);
                    arrayBuilder.AddRange(values);
                    this.dictionary[k] = arrayBuilder;
                }
                else
                {
                    arrayBuilder.AddRange(values);
                }
            }
            else
            {
                this.EnsureDictionary();

                if (values.Length == 1)
                {
                    this.dictionary[k] = values[0];
                }
                else
                {
                    arrayBuilder = new ArrayBuilder<V>(values.Length);
                    arrayBuilder.AddRange(values);
                    this.dictionary[k] = arrayBuilder;
                }
            }
        }

        /// <summary>
        /// Get the number of values assocaited with a key.
        /// </summary>
        public int GetCountForKey(K k)
        {
            object item;
            if (!this.IsEmpty && this.dictionary.TryGetValue(k, out item))
            {
                var arrayBuilder = item as ArrayBuilder<V>;
                return arrayBuilder != null ? arrayBuilder.Count : 1;
            }
            return 0;
        }

        /// <summary>
        /// Returns true if one or more items with given key have been added.
        /// </summary>
        public bool ContainsKey(K k)
        {
            return !this.IsEmpty && this.dictionary.ContainsKey(k);
        }

        /// <summary>
        /// Get all values associated with K, in the order they were added.
        /// Returns empty read-only array if no values were present.
        /// </summary>
        public ImmutableArray<V> this[K k]
        {
            get
            {
                object item;
                if (!this.IsEmpty && this.dictionary.TryGetValue(k, out item))
                {
                    var arrayBuilder = item as ArrayBuilder<V>;
                    if (arrayBuilder == null)
                    {
                        // promote singleton to set
                        Debug.Assert(item is V, "Item must be either a V or an ArrayBuilder<V>");
                        return ImmutableArray.Create<V>((V)item);
                    }
                    else
                    {
                        return arrayBuilder.ToImmutable();
                    }
                }

                return ImmutableArray<V>.Empty;
            }
        }

        /// <summary>
        /// Get a collection of all the keys.
        /// </summary>
        public ICollection<K> Keys
        {
            get { return this.IsEmpty ? SpecializedCollections.EmptyCollection<K>() : this.dictionary.Keys; }
        }
    }
}
