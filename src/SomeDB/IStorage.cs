using System;
using System.Collections.Generic;

namespace SomeDB
{
    // todo read/write concurrency
    // todo contextual get/set

    public interface IStorage
    {

        void Store(Type type, string id, string value);
        string Retrieve(Type type, string id);
        IEnumerable<string> RetrieveAll(Type type);
        void Remove(Type type, string id);
        void CopyTo(IStorage otherStorage);

    }
}
