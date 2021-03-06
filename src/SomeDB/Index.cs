﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SomeDB
{
    public abstract class Index
    {
        public Type Type { get; private set; }

        protected Index(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            Type = type;
        }

        internal abstract void Remove(DocId docId);
        internal abstract void Update(IDocument value);
    }

    public abstract class Index<TDoc> : Index
    {
        protected Index() : base(typeof(TDoc)) { }
    }

    public class Index<TDoc, TKey> : Index<TDoc> where TDoc : IDocument
    {
        // TODO handle null keys
        // TODO disk save/load/population

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly Func<TDoc, TKey> _keyFactory;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly Func<TDoc, bool> _predicate;
        private readonly Dictionary<DocId, TKey> _idMap = new Dictionary<DocId, TKey>();
        private readonly Dictionary<TKey, HashSet<DocId>> _idx = new Dictionary<TKey, HashSet<DocId>>();

        public Index(Func<TDoc, TKey> keyFactory, Func<TDoc, bool> predicate = null, IEqualityComparer<TKey> keyComparer = null)
        {
            if (keyFactory == null) throw new ArgumentNullException("keyFactory");
            _keyFactory = keyFactory;
            _keyComparer = keyComparer;
            _predicate = predicate;
        }

        internal void Update(TDoc doc)
        {
            if (_predicate != null && !_predicate(doc))
                return;

            var id = new DocId(doc);
            var newKey = _keyFactory(doc);

            _lock.EnterReadLock();
            try
            {
                // check update needed
                if (_idMap.ContainsKey(id))
                {
                    var oldKey = _idMap[id];
                    if (KeysAreEqual(oldKey, newKey) && (_idx.ContainsKey(newKey) && _idx[newKey].Contains(id)))
                        return;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                // check update needed
                var oldKey = default(TKey);
                var hasOldKey = _idMap.ContainsKey(id);
                if (hasOldKey)
                {
                    oldKey = _idMap[id];
                    if (KeysAreEqual(oldKey, newKey) && (_idx.ContainsKey(newKey) && _idx[newKey].Contains(id)))
                        return;
                }

                // add new index data
                _idMap[id] = newKey;

                if (_idx.ContainsKey(newKey))
                    _idx[newKey].Add(id);
                else
                    _idx[newKey] = new HashSet<DocId>(new[] {id});

                if (!hasOldKey) return;

                // remove old index data
                if (_idx[oldKey].Count == 1)
                    _idx.Remove(oldKey);
                else
                    _idx[oldKey].Remove(id);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal override void Remove(DocId id)
        {
            _lock.EnterReadLock();
            try
            {
                if (!_idMap.ContainsKey(id))
                    return;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            _lock.EnterWriteLock();
            try
            {
                if (!_idMap.ContainsKey(id))
                    return;

                var key = _idMap[id];

                if (_idx.ContainsKey(key))
                {
                    if (_idx[key].Count == 1)
                        _idx.Remove(key);
                    else
                        _idx[key].Remove(id);
                }

                _idMap.Remove(id);

            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal override void Update(IDocument value)
        {
            Update((TDoc)value);
        }

        internal DocId[] Query(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                return _idx.ContainsKey(key)
                    ? _idx[key].ToArray()
                    : new DocId[0];
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public DocId[] Query(Func<TKey, bool> predicate)
        {
            _lock.EnterReadLock();
            try
            {
                return _idx
                    .Where(x => predicate == null || predicate(x.Key))
                    .SelectMany(x => x.Value)
                    .ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private bool KeysAreEqual(TKey key1, TKey key2)
        {
            // need same equality comparison semantics as Dictionary<TKey,TAnything>
            var dict = _keyComparer == null
                ? new Dictionary<TKey, object>()
                : new Dictionary<TKey, object>(_keyComparer);

            dict[key1] = null;
            return dict.ContainsKey(key2);
        }
    }
}
