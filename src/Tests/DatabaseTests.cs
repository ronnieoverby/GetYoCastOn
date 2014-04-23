using System;
using System.Linq;
using CoreTechs.Common;
using NUnit.Framework;
using SomeDB;

namespace Tests
{
    public class DatabaseTests
    {
        const string Dir = @"c:\somedb\";

        [Test]
        public void LoadTest()
        {
            var db = new Database(Dir);
            var ronnie = db.Load<Person>("364f9d08-dcfe-45a3-a75a-f25164d1f933");
            Assert.That(ronnie, Is.Not.Null);
            Console.WriteLine(ronnie.Name);
            Console.WriteLine(ronnie.Age);
        }

        [Test]
        public void QueryTest()
        {
            var db = new Database(Dir);
            var ronnie = db.Query<Person>().Where(x => x.Name == "Ronnie").ToArray();
            var avgage = ronnie.Average(x => x.Age);
            avgage.Noop();
            ronnie.Noop();

        }

        [Test]
        public void SaveTest()
        {
            var db = new Database(Dir);
            var res = db.Save(new Person
            {
                Name = "Harry",
                Age = 29
            });

            Assert.That(res, Is.Not.Null);

            Console.WriteLine(res.File);
            Console.WriteLine(res.Id);

        }

        [Test]
        public void DeleteByIdTest()
        {
            var db = new Database(Dir);
            db.DeleteById<Person>("9825e0f7-8d54-4f22-9069-b1e941a67876");
        }

        [Test]
        public void DeleteWhereTest()
        {
            var db = new Database(Dir);
            db.DeleteWhere<Person>(x => x.Name == "Harry");
        }

        [Test]
        public void DeleteAllTest()
        {
            var db = new Database(Dir);
            db.DeleteWhere<Person>();
        }

        [Test]
        public void CanCreateDeterministicIdFromState()
        {
            var p = new Person {Age = 29, Name = "Ronnie"};
            var id = IdDeriver.DeriveIdFromState(p);
            var id2 = IdDeriver.DeriveIdFromState(p);
            p.Age += 1;
            var id3 = IdDeriver.DeriveIdFromState(p);

            Assert.That(id, Is.EqualTo(id2));
            Assert.That(id, Is.Not.EqualTo(id3));
        }

        [Test]
        public void CanGetStringIdFromObject()
        {
            var p = new Person {Id = "abc"};
            var id = p.DeriveId();
            Assert.That(id, Is.EqualTo(p.Id));
        }

        [Test]
        public void StringIdSetWhenMissing()
        {
            var p = new Person();
            var id = p.DeriveId();
            Assert.That(id, Is.Not.Empty.Or.Null);
            Assert.That(id, Is.EqualTo(p.Id));
        }

        [Test]
        public void GuidIdSetWhenMissing()
        {
            var p = new Rec1();
            var id = p.DeriveId();
            Assert.That(id, Is.EqualTo(p.Id).And.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void NullableGuidIdSetWhenMissing()
        {
            var p = new Rec2();
            var id = p.DeriveId();
            Assert.That(id, Is.EqualTo(p.Id).And.Not.EqualTo(Guid.Empty).And.Not.Null);
        }

        [Test]
        public void CanGetGuidIdFromObject()
        {
            var id = "Ronnie".CreateDeterministicGuid();
            var p = new Rec1 { Id = id };
            var id2 = p.DeriveId();
            Assert.That(id, Is.EqualTo(p.Id).And.EqualTo(id2));
        }

        [Test]
        public void CanGetNullableGuidIdFromObject()
        {
            var id = "Ronnie".CreateDeterministicGuid();
            var p = new Rec2 { Id = id };
            var id2 = p.DeriveId();
            Assert.That(id, Is.EqualTo(p.Id).And.EqualTo(id2));
        }

        [Test]
        public void CanGetIntIdFromObject()
        {
            const int value = 123;
            var p = new Rec3 {Id = value};
            var id = p.DeriveId();
            Assert.That(id, Is.EqualTo(p.Id).And.EqualTo(value));
        }

        [Test]
        public void IntIdWontBeSetOnObjectWhenMissing()
        {
            var p = new Rec3();
            var id = p.DeriveId();
            Assert.That(id, Is.TypeOf<Guid>());
            Assert.That(p.Id, Is.Null);
        }
    }

    public class Rec1
    {
        public Guid Id { get; set; }
    }

    public class Rec2
    {
        public Guid? Id { get; set; }
    }

    public class Rec3
    {
        public int? Id { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}