using System;
using System.Linq;
using CoreTechs.Common;
using NUnit.Framework;
using SomeDB;

namespace Tests
{
    public class DbTest
    {
        [Test]
        public void Test()
        {
            var config = DatabaseConfig.CreateDefault();

            // create case insensitive index of people's names who are no younger than 13

            var index = new Index<Person, string>(
                x => x.Name,
                x => x.Age >= 13,
                StringComparer.OrdinalIgnoreCase);

            config.Indexes.Add(index);

            var db = new Database(config);

         
            var ronnies = db.Query(index, name => name.StartsWith("Ronnie"));

            ronnies.Noop();
            var ronz = db.GetEnumerable<Person>().Where(x => x.Age == 29).ToList();
            ronz.Noop();

            foreach (var p in db.GetEnumerable<Person>())
                db.Delete<Person>(p.Id);

            db.Update<Person>(x => x.Name = "Sanders", x => x.Name == "Manders");
        }

    }

    class Person : IDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}