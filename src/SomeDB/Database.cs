using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SomeDB.Storage;

namespace SomeDB
{
    public class Database : IDisposable
    {
        private readonly IStorage _storage;
        private readonly IList<Index> _indexes;
        private readonly IIdFactory _idFactory;
        public Stats Stats { get; set; }

        public Database()
            : this(DatabaseConfig.CreateDefault())
        {
        }

        public Database(DatabaseConfig config)
            : this(config.Storage, config.Indexes, config.IdFactory, config.Stats)
        {
        }

        public Database(IStorage storage, IEnumerable<Index> indexes = null, IIdFactory idFactory = null, Stats stats = null)
        {
            Stats = stats;
            if (storage == null) throw new ArgumentNullException("storage");
            _storage = storage;
            _idFactory = idFactory ?? new GuidIdFactory();

            _indexes = (indexes ?? new Index[0]).ToList();
            InitIndexes(storage);
        }

        private void InitIndexes(IStorage storage)
        {
            if (!_indexes.Any()) return;
            foreach (var document in storage.RetrieveAll())
            {
                var sw = Stopwatch.StartNew();
                UpdateIndexes(document);
                Stats.Record("Init Index", sw.Elapsed);
            }
        }

        private readonly ConcurrentDictionary<Type, Type[]> _subTypeCache = new ConcurrentDictionary<Type, Type[]>();

        public void SaveMany(IEnumerable<IDocument> documents)
        {
            if (documents == null) throw new ArgumentNullException("documents");

            foreach (var doc in _storage.Store(documents.Select(EnsureIdAssigned)))
                UpdateIndexes(doc);
        }

        private void UpdateIndexes(IDocument doc)
        {
            foreach (var index in GetIndexes(doc.GetType()))
                index.Update(doc);
        }

        private readonly ConcurrentDictionary<Type, Index[]> _indexTypeCache = new ConcurrentDictionary<Type, Index[]>();

        private Index[] GetIndexes(Type type)
        {
            return _indexTypeCache.GetOrAdd(type, t =>
            {
                var types = type.GetAllSuperTypes()
                    .Where(x => x != typeof(IDocument) && typeof(IDocument).IsAssignableFrom(x)).ToList();
                types.Add(type);

                return _indexes.Where(i => types.Any(t2 => i.Type.IsAssignableFrom(t2))).ToArray();

            });
        }

        private IDocument EnsureIdAssigned(IDocument doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(doc.Id))
                _idFactory.AssignNewId(doc);

            Stats.Record("EnsureIdAssigned", sw.Elapsed);

            return doc;
        }

        public void Save(IDocument document)
        {
            if (document == null) throw new ArgumentNullException("document");
            SaveMany(new[] { document });
        }

        internal T Load<T>(DocId id) where T : IDocument
        {
            if (id == null) throw new ArgumentNullException("id");
            return (T)Load(id.Id, id.Type);
        }

        public T Load<T>(string id) where T : IDocument
        {
            if (id == null) throw new ArgumentNullException("id");
            return (T)Load(id, typeof(T));
        }

        public IDocument Load(string id, Type type)
        {
            if (id == null) throw new ArgumentNullException("id");
            return _storage.Retrieve(type, id);
        }

        public void Delete<T>(string id) where T : IDocument
        {
            if (id == null) throw new ArgumentNullException("id");
            Delete(typeof(T), id);
        }

        public void Delete(IDocument document)
        {
            if (document == null) throw new ArgumentNullException("document");
            Delete(document.GetType(), document.Id);
        }

        public void Delete(IEnumerable<IDocument> documents)
        {
            if (documents == null) throw new ArgumentNullException("documents");
            foreach (var doc in documents)
                Delete(doc.GetType(), doc.Id);
        }

        public void Delete(Type type, string id)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");

            _storage.Remove(type, id);
            foreach (var index in GetIndexes(type))
                index.Remove(new DocId(type, id));
        }

        private IEnumerable<IDocument> GetEnumerable(Type type)
        {
            var subTypes = _subTypeCache.GetOrAdd(type, t => t.GetAllSubTypes());
            var types = new[] { type }.Concat(subTypes);

            return from t in types
                   from doc in _storage.RetrieveAll(t)
                   where doc != null
                   select doc;
        }

        public IEnumerable<T> GetEnumerable<T>() where T : IDocument
        {
            return GetEnumerable(typeof(T)).Cast<T>();
        }

        public IEnumerable<IDocument> GetEnumerable()
        {
            return _storage.RetrieveAll();
        }

        public IEnumerable<TDoc> Query<TDoc, TKey>(Index<TDoc, TKey> index, Func<TKey, bool> indexPredicate = null, Func<TDoc, bool> documentPredicate = null)
            where TDoc : IDocument
        {
            if (index == null) throw new ArgumentNullException("index");

            if (!IsAttached(index))
                throw new ArgumentException(
                    "That index is not attached. Attach all indexes at the time of database construction.", "index");

            var ids = index.Query(indexPredicate);
            return ids.Select(Load<TDoc>).Where(x => documentPredicate == null || documentPredicate(x));
        }

        private bool IsAttached(Index index)
        {
            return _indexes.Contains(index);
        }

        public void Update<T>(Action<T> updateAction, Func<T, bool> predicate = null) where T : IDocument
        {
            if (updateAction == null) throw new ArgumentNullException("updateAction");

            foreach (var item in GetEnumerable<T>().Where(item => predicate == null || predicate(item)))
            {
                updateAction(item);
                Save(item);
            }
        }

        public void Update<TDoc, TKey>(Index<TDoc, TKey> index, Action<TDoc> updateAction, Func<TKey, bool> indexPredicate = null, Func<TDoc, bool> documentPredicate = null) where TDoc : IDocument
        {
            if (index == null) throw new ArgumentNullException("index");
            if (updateAction == null) throw new ArgumentNullException("updateAction");

            foreach (var item in Query(index, indexPredicate, documentPredicate))
            {
                updateAction(item);
                Save(item);
            }
        }

        public void Delete<TDoc, TKey>(Index<TDoc, TKey> index, Func<TKey, bool> indexPredicate = null, Func<TDoc, bool> documentPredicate = null) where TDoc : IDocument
        {
            if (index == null) throw new ArgumentNullException("index");

            foreach (var item in Query(index, indexPredicate, documentPredicate))
                Delete(item);
        }

        public void Dispose()
        {
            var disp = _storage as IDisposable;
            if (disp != null)
                disp.Dispose();
        }
    }
}
