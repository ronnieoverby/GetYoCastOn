using System;
using System.Linq;
using NUnit.Framework;
using SomeDB;
using SomeDB.Storage;

namespace Tests
{
    class PolyTests
    {
        [Test]
        public void PolymorphicEnumeration()
        {
            var stgs = new IStorage[]
            {
                new MemoryStorage(),
                new PersistedMemoryStorage(),
                new FileSystemStorage("fs"),
                new EsentStorage("esent"),
            };

            foreach (var stg in stgs)
            {
                using (var db = new Database(stg, null, new GuidIdFactory()))
                {
                    db.Delete(db.GetEnumerable());
                    db.SaveMany(new Animal[]
                    {
                        new Cat(), new Dog(), new Cow(),
                    }.AsEnumerable());

                    var animals = db.GetEnumerable<Animal>();
                    Assert.That(animals.Count(), Is.EqualTo(3), "{0} not polymorphic", stg.GetType().Name);
                }
            }
        }

        [Test,Repeat(10)]
        public void PolyQueries()
        {
            var stgs = new IStorage[]
            {
                new MemoryStorage(),
                new PersistedMemoryStorage(),
                new FileSystemStorage("fs"),
                new EsentStorage("esent"),
            };

            foreach (var stg in stgs)
            {
                var idx = new Index<Animal, string>(a => a.Sound, keyComparer: StringComparer.OrdinalIgnoreCase);

                using (var db = new Database(stg, indexes: new[] { idx }, idFactory: new GuidIdFactory()))
                {
                    db.Delete(db.GetEnumerable());

                    db.SaveMany(new Animal[]
                    {
                        new Cat(), new Dog(), new Cow(),
                    }.AsEnumerable());

                    var animals = db.Query(idx);
                    Assert.That(animals.Count(), Is.EqualTo(3), "{0} not polymorphic", stg.GetType().Name);
                }
            }
        }
    }

    abstract class Animal : IDocument
    {

        abstract public string Sound { get; }
        public string Id { get; set; }
    }

    internal class Cow : Animal
    {
        public override string Sound
        {
            get { return "Mooo"; }
        }
    }

    internal class Cat : Animal
    {
        public override string Sound
        {
            get { return "Meow"; }
        }
    }

    internal class Dog : Animal
    {
        public override string Sound
        {
            get { return "Woof"; }
        }
    }
}
