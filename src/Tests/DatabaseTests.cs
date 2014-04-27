using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreTechs.Common;
using NUnit.Framework;
using SomeDB;
using SomeDB.Storage;

namespace Tests
{
    public class DbTest
    {
        [Test]
        public void Test()
        {
            var config = DatabaseConfig.CreateDefault();

            var index = new Index<Person, string>(
                x => x.Name,
                x => x.Age >= 13,
                StringComparer.OrdinalIgnoreCase);

            config.Indexes.Add(index);

            var index2 = new Index<Person, int>(x => x.Age);
            config.Indexes.Add(index2);

            var db = new Database(config);

            var under13 = db.Query(index2, x => x < 13).ToArray();
            under13.Noop();
        }
    }

    static class Ext
    {
        public static T Random<T>(this IList<T> items)
        {
            return items[RNG.Next(items.Count)];
        }
    }
    class Person : IDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        static public IEnumerable<Person> MakeMany()
        {
            var names = new[] { "Ronnie", "Tina", "Anna", "Lukus", "Shane", "Walter", "Bob", "Brandon", "Mikey", "Morgan", "Wheeler", "Matt", "Horrace", "Willy", "Bill", "Dick", "Andrew", "Tyler", "Markus" };
            var surNames = new[] { "Overby", "Smith", "Darling", "Tamriel", "Leonard", "Dyson", "Samson", "Jackson", "Davis", "Namis", "Johnston" };
            while (true)
            {
                yield return new Person
                {
                    Age = RNG.Next(100),
                    Name = string.Format("{0} {1}", names.Random(), surNames.Random())
                };
            }
        }
    }
}