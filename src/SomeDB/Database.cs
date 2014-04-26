using System;
using System.Collections.Generic;
using System.Linq;

namespace SomeDB
{
    // TODO polymorphic queries

    public class Database
    {
        private readonly IStorage _storage;
        private readonly ISerializer _serializer;
        private readonly Func<Type, string> _idFactory;
        private readonly ILookup<Type, Index> _indexes;

        public Database()
            : this(BuildDefaultConfig())
        {
        }

        private static DatabaseConfig BuildDefaultConfig()
        {
            return new DatabaseConfig
            {
                Serializer = new JsonSerializer(),
                Storage = new PersistedMemoryStorage()
            };
        }

        public Database(DatabaseConfig config)
            : this(config.Storage, config.Serializer, config.Indexes, config.IdFactory)
        {
        }

        private Database(IStorage storage, ISerializer serializer, IEnumerable<Index> indexes, Func<Type, string> idFactory)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (serializer == null) throw new ArgumentNullException("serializer");
            _storage = storage;
            _serializer = serializer;
            _idFactory = idFactory;

            if (indexes != null)
                _indexes = indexes.ToLookup(x => x.Type);

            foreach (var type in _indexes.Select(x => x.Key))
                foreach (var value in GetEnumerable(type))
                    foreach (var index in _indexes[type])
                        index.Update(value);
        }

        private IEnumerable<IDocument> GetEnumerable(Type type)
        {
            // todo make polymorphic
            foreach (var serialized in _storage.RetrieveAll(type))
            {
                var value = _serializer.Deserialize(serialized, type);
                yield return (IDocument)value;
            }
        }

        public void Purge()
        {
            _storage.Purge();
        }

        public void Save<T>(IEnumerable<T> values) where T : IDocument
        {
            if (values == null) throw new ArgumentNullException("values");
            foreach (var item in values)
                Save(item);
        }

        public void Save<T>(T value) where T : IDocument
        {
            if (value == null) throw new ArgumentNullException("value");

            var type = value.GetType();
            
            if (string.IsNullOrWhiteSpace(value.Id))
                value.Id = _idFactory(type);

            var serialized = _serializer.Serialize(value);
            _storage.Store(type, value.Id, serialized);

            // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
            foreach (Index<T> index in _indexes[type])
                index.Update(value);
        }

        public T Load<T>(string id) where T : IDocument
        {
            if (id == null) throw new ArgumentNullException("id");

            var type = typeof(T);
            var serialized = _storage.Retrieve(type, id);
            return (T)_serializer.Deserialize(serialized, type);
        }

        public void Delete<T>(string id) where T : IDocument
        {
            if (id == null) throw new ArgumentNullException("id");
            Delete(typeof(T), id);
        }

        public void Delete(IDocument value)
        {
            if (value == null) throw new ArgumentNullException("value");
            Delete(value.GetType(), value.Id);
        }

        public void Delete(IEnumerable<IDocument> values)
        {
            if (values == null) throw new ArgumentNullException("values");
            foreach (var doc in values)
                Delete(doc.GetType(), doc.Id);
        }

        public void Delete(Type type, string id)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");

            _storage.Remove(type, id);
            foreach (var index in _indexes[type])
                index.Remove(id);
        }

        public IEnumerable<T> GetEnumerable<T>() where T : IDocument
        {
            return GetEnumerable(typeof(T)).Cast<T>();
        }

        public IEnumerable<TDoc> Query<TDoc, TKey>(Index<TDoc, TKey> index, Func<TKey, bool> indexPredicate = null, Func<TDoc, bool> documentPredicate = null)
            where TDoc : IDocument
        {
            if (index == null) throw new ArgumentNullException("index");

            var ids = index.Query(indexPredicate);
            return ids.Select(Load<TDoc>).Where(x => documentPredicate == null || documentPredicate(x));
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
    }
}
