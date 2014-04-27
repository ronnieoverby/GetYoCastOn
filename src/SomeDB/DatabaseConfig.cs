using System.Collections.Generic;
using SomeDB.Storage;

namespace SomeDB
{
    public class DatabaseConfig
    {
        public IStorage Storage { get; set; }
        public IList<Index> Indexes { get; set; }
        public IIdFactory IdFactory { get; set; }
        public Stats Stats { get; set; }

        public DatabaseConfig()
        {
            Indexes = new List<Index>();
        }

        public static DatabaseConfig CreateDefault()
        {
            return new DatabaseConfig
            {
                Storage = new EsentStorage(),
                IdFactory = new GuidIdFactory(),
            };
        }
    }
}