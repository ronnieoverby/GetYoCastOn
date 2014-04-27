using System;
using System.Collections.Generic;
using System.Linq;
using CoreTechs.Common;
using NUnit.Framework;
using SomeDB;
using SomeDB.Storage;

namespace Tests
{
    public class DbTest
    {
        [Test]
        public void CanStoreDocument()
        {
            var db = new Database(new MemoryStorage());
            db.Save(new Person());
            db.GetEnumerable<Person>().Single();
        }

        [Test]
        public void CanIndexDocument()
        {
            var idx = new Index<Person, string>(x => x.Name);
            var db = new Database(new MemoryStorage(), new[] { idx });
            db.Save(new Person { Name = "Ronnie" });
            db.Query(idx).Single();
        }

        [Test]
        public void Test()
        {
            var wallwalla = "wallwalla";

            Func<IStorage> makeStorage = () => new EsentStorage(wallwalla);

            /*  using (var db = new Database(makeStorage()))
                  db.Delete(db.GetEnumerable());*/

            var idxAgeGender = new Index<Person, dynamic>(x => new { x.Age, x.Gender });


            using (var db = new Database(makeStorage(), new[] { idxAgeGender }, stats: new Stats()))
            {
                db.SaveMany(Person.MakeMany().Take(100));

                const double age = 29;
                var min = Math.Ceiling(age / 2 + 7);
                var max = age + (age - min);

                var ladies = db.Query(idxAgeGender, x => x.Age >= min && x.Age <= max && x.Gender == Gender.Female);

              /*  foreach (var lady in ladies.OrderBy(x => x.Age))
                    Console.WriteLine("{0} ({1})", lady.Name, lady.Age);*/

                Console.WriteLine(db.Stats.ToString());
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
        public Gender Gender { get; set; }

        static public IEnumerable<Person> MakeMany()
        {
            var boyNames = new[] { "Ronnie", "Milton", "Andy", "Lukus", "Shane", "Walter", "Bob", "Brandon", "Mikey", "Morgan", "Wheeler", "Matt", "Hank", "Willy", "Bill", "Dick", "Andrew", "Tyler", "Markus" };
            var girlNames = new[] { "Rhonda", "Tina", "Anna", "Lilly", "Shannon", "Willa", "Bobbie", "Phillis", "Megan", "Morgan", "Wilma", "Mildred", "Horrace", "Betty", "Sandra", "Darlene", "Amanda", "Tammy", "Angelina" };
            var surNames = new[] { "Overby", "Smith", "Darling", "Tamriel", "Leonard", "Dyson", "Samson", "Jackson", "Davis", "Namis", "Johnston" };
            while (true)
            {
                var gender = RNG.NextBool() ? Gender.Male : Gender.Female;
                var names = gender == Gender.Female ? girlNames : boyNames;

                yield return new Person
                {
                    Age = RNG.Next(121),
                    Name = string.Format("{0} {1}", names.Random(), surNames.Random()),
                    Gender = gender
                };
            }
        }
    }

    class Child : Person
    {

    }

    internal enum Gender
    {
        Male, Female
    }
}