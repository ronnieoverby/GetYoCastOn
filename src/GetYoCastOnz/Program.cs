using System;
using System.Linq;
using CoreTechs.Common;
using SomeDB;
using SomeDB.Storage;

namespace GetYoCastOn
{
    public class Program
    {
        private Database _db;


        public Program()
        {
            var config = DatabaseConfig.CreateDefault();
            config.Storage = new FileSystemStorage("data");
            _db = new Database(config);
        }

        private static void Main()
        {
            new Program().Run();
        }

        private void Run()
        {
            while (true)
            {
                Console.WriteLine("Choose wisely:");
                Console.WriteLine("1.) View Podcasts");
                Console.WriteLine("2.) Add Podcast");
                Console.WriteLine("3.) Remove Podcast");

                switch (Console.ReadLine().AttemptGet(int.Parse).Value)
                {
                    case 1:
                        ViewCasts();
                        break;
                    case 2:
                        AddCast();
                        break;
                    case 3:
                        RemoveCast();
                        break;
                }
            }
        }

        private void RemoveCast()
        {
            Console.WriteLine("Title?");
            var title = Console.ReadLine();
            var results =
                _db.GetEnumerable<Podcast>().Where(x => x.Title.Contains(title, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (!results.Any())
                Console.WriteLine("No Results");
            else
            {
                Console.WriteLine("Select one to remove:");

                for (int i = 0; i < results.Length; i++)
                    Console.WriteLine("{0}: {1}", i, results[i].Title);

                var selection = Console.ReadLine().AttemptGet(int.Parse).Value.AttemptGet(i => results[i]);

                if (!selection.Succeeded) return;

                _db.Delete(selection.Value);
                Console.WriteLine("Deleted {0}", selection.Value.Title);
            }
        }

        private void AddCast()
        {
            Console.WriteLine("Feed Url?");
            var attempt = Console.ReadLine().AttemptGet(x => new Podcast(x));
            if (attempt.Succeeded)
            {
                _db.Save(attempt.Value);
                Console.WriteLine("Added {0}", attempt.Value.Title);
            }

        }

        private void ViewCasts()
        {
            foreach (var cast in _db.GetEnumerable<Podcast>().OrderBy(x => x.Title))
                Console.WriteLine(cast.Title);
        }

        private void Seed()
        {
            _db.Save(new PodcastSettings
            {
                Id = "global",
                SplitLength = TimeSpan.FromSeconds(45),
                TempoMultiplier = 1.25,
            });

            var casts = new[]
            {
                new Podcast("http://feeds2.feedburner.com/HerdingCode?fmt=xml"),
                new Podcast("http://feeds.feedburner.com/netRocksFullMp3Downloads?fmt=xml"),
            };

            foreach (var cast in casts.Where(cast => _db.GetEnumerable<Podcast>().All(x => x.Title != cast.Title)))
                _db.Save(cast);
        }
    }
}