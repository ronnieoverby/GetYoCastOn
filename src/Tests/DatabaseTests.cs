using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            config.Storage = new FileSystemStorage();

            var index = new Index<Person, string>(
                x => x.Name,
                x => x.Age >= 13,
                StringComparer.OrdinalIgnoreCase);

            config.Indexes.Add(index);

            var db = new Database(config);

            Task.Run(() => db.Save(Person.MakeMany()));
            while (true)
            {
                var sw = Stopwatch.StartNew();
                var ronnies = db.GetEnumerable<Person>().Where(x => x.Name == "Ronnie Overby" && x.Age == 29).ToArray();
                ronnies.Noop();
                Console.WriteLine(sw.Elapsed);
                Console.WriteLine(ronnies.Length);
                sw.Restart();
                ronnies = db.Query(index, name => name == "Ronnie Overby", x => x.Age == 29).ToArray();
                Console.WriteLine(sw.Elapsed);
                Console.WriteLine(ronnies.Length);
                ronnies.Noop(); 
            }
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