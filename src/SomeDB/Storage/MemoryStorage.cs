using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SomeDB.Storage
{
    public class MemoryStorage : IStorage
    {
        private readonly ConcurrentDictionary<DocId, IDocument> _data =
            new ConcurrentDictionary<DocId, IDocument>();


        public IEnumerable<IDocument> Store(IEnumerable<IDocument> documents)
        {
            foreach (var document in documents)
            {
                var key = new DocId(document);
                _data[key] = document;
                yield return document;
            }
        }

        public IDocument Retrieve(Type type, string id)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (id == null) throw new ArgumentNullException("id");

            var key = new DocId(type, id);
            IDocument value;
            _data.TryGetValue(key, out value);
            return value;
        }

        public IEnumerable<IDocument> RetrieveAll(Type type)
        {
            return _data.Where(x => type == x.Key.Type).Select(x => x.Value);
        }

        public IEnumerable<IDocument> RetrieveAll()
        {
            return _data.Values;
        }

        public void Remove(Type type, string id)
        {
            IDocument value;
            var key = new DocId(type, id);
            _data.TryRemove(key, out value);
        }

        public void Purge()
        {
            _data.Clear();
        }

        public void Purge(Type type)
        {
            foreach (var document in _data.Where(document => type.IsAssignableFrom(document.Key.Type)))
            {
                IDocument value;
                _data.TryRemove(document.Key, out value);
            }
        }
    }
}