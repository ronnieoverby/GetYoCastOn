using System;
using System.Collections.Generic;

namespace SomeDB.Storage
{
    public class PersistedMemoryStorage : IPersistentStorage
    {
        private readonly IStorage _mem = new MemoryStorage();
        private readonly IPersistentStorage _fs;

        public PersistedMemoryStorage(IPersistentStorage persistentStorage = null)
        {
            _fs = persistentStorage ?? new EsentStorage();
            _fs.CopyTo(_mem);
        }

        public IEnumerable<IDocument> Store(IEnumerable<IDocument> documents)
        {
            return _mem.Store(_fs.Store(documents));
        }

        public IDocument Retrieve(Type type, string id)
        {
            var value = _mem.Retrieve(type, id);
            return value;
        }

        public IEnumerable<IDocument> RetrieveAll(Type type)
        {
            return _mem.RetrieveAll(type);
        }

        public IEnumerable<IDocument> RetrieveAll()
        {
            return _mem.RetrieveAll();
        }

        public void Remove(Type type, string id)
        {
            _fs.Remove(type, id);
            _mem.Remove(type, id);
        }
    }
}