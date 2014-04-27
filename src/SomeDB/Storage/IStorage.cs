using System;
using System.Collections.Generic;

namespace SomeDB.Storage
{
    // todo read/write concurrency

    public interface IStorage
    {
        void Store(Type type, string id, string value);
        string Retrieve(Type type, string id);
        IEnumerable<string> RetrieveAll(Type type);
        void Remove(Type type, string id);
        void CopyTo(IStorage otherStorage);
        void Purge();
        void Purge(Type type);
    }
}
