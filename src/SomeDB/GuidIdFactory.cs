using System;

namespace SomeDB
{
    public class GuidIdFactory : IIdFactory
    {
        public void AssignNewId(IDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException("doc");

            doc.Id = Guid.NewGuid().ToString();
        }
    }
}