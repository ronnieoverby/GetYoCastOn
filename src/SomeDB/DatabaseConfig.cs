using System;
using System.Collections.Generic;

namespace SomeDB
{
    public class DatabaseConfig
    {
        public IStorage Storage { get; set; }
        public ISerializer Serializer { get; set; }
        public IList<Index> Indexes { get; set; }
        public Func<string> IdFactory { get; set; }

        public DatabaseConfig()
        {
            Indexes = new List<Index>();
        }

        public static DatabaseConfig CreateDefault()
        {
            return new DatabaseConfig
            {
                Serializer = new JsonSerializer(),
                Storage = new PersistedMemoryStorage(),
                IdFactory = () => Guid.NewGuid().ToString()
            };
        }
    }
}