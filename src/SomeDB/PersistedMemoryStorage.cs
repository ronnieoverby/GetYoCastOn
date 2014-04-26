using System;
using System.Collections.Generic;

namespace SomeDB
{
    public class PersistedMemoryStorage : IStorage
    {
        private readonly IStorage _mem = new MemoryStorage();
        private readonly IStorage _fs;

        public PersistedMemoryStorage()
        {
            _fs = new FileSystemStorage();
            _fs.CopyTo(_mem);
        }

        public PersistedMemoryStorage(string rootPath)
        {
            _fs = new FileSystemStorage(rootPath);
            _fs.CopyTo(_mem);
        }

        public void Store(Type type, string id, string value)
        {
            _fs.Store(type, id, value);
            _mem.Store(type, id, value);
        }

        public string Retrieve(Type type, string id)
        {
            var value = _mem.Retrieve(type, id);
            return value;
        }

        public IEnumerable<string> RetrieveAll(Type type)
        {
            return _mem.RetrieveAll(type);
        }

        public void Remove(Type type, string id)
        {
            _fs.Remove(type, id);
            _mem.Remove(type, id);
        }

        public void CopyTo(IStorage otherStorage)
        {
            _mem.CopyTo(otherStorage);
        }

        public void Purge()
        {
            _fs.Purge();
            _mem.Purge();
        }

        public void Purge(Type type)
        {
            _mem.Purge(type);
            _fs.Purge(type);
        }
    }
}