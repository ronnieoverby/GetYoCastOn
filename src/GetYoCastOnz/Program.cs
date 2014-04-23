using System;
using System.Linq;
using SomeDB;

namespace GetYoCastOn
{
    public class Program
    {
        private static void Main()
        {
            var db = new Database("data");
            Seed(db);

        }

        private static void Seed(Database db)
        {
            db.Save(new PodcastSettings
            {
                SplitLength = TimeSpan.FromSeconds(45),
                TempoMultiplier = 1.25
            }, "global");
            var casts = new[]
            {
                new Podcast("http://feeds2.feedburner.com/HerdingCode?fmt=xml"),
                new Podcast("http://feeds.feedburner.com/netRocksFullMp3Downloads?fmt=xml"),
            };

            foreach (var cast in casts.Where(cast => !db.Query<Podcast>().Any(x => x.Title == cast.Title)))
                db.Save(cast);
        }
    }
}