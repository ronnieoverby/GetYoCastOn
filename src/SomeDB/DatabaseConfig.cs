using System.Collections.Generic;
using SomeDB.Storage;

namespace SomeDB
{
    public class DatabaseConfig
    {
        public IStorage Storage { get; set; }
        public ISerializer Serializer { get; set; }
        public IList<Index> Indexes { get; set; }
        public IIdFactory IdFactory { get; set; }

        public DatabaseConfig()
        {
            Indexes = new List<Index>();
        }

        public static DatabaseConfig CreateDefault()
        {
            return new DatabaseConfig
            {
                Serializer = new JsonSerializer(),
                Storage = new FileSystemStorage(),
                IdFactory = new GuidIdFactory()
            };
        }
    }
}