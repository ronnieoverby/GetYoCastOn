using System;
using System.Collections.Generic;

namespace SomeDB.Storage
{
    // todo read/write concurrency

    public interface IStorage
    {
        IEnumerable<IDocument> Store(IEnumerable<IDocument> documents);
        IDocument Retrieve(Type type, string id);
        IEnumerable<IDocument> RetrieveAll(Type type);
        IEnumerable<IDocument> RetrieveAll();
        void Remove(Type type, string id);
    }

    public interface IPersistentStorage : IStorage
    {
        // marker interfaces for persistent storage services
    }
}
