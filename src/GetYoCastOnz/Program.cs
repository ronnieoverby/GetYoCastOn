using System;
using System.IO;
using System.Linq;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Database.Server;

namespace GetYoCastOn
{
    public class Program
    {
        private readonly IDocumentStore _docStore;

        private Program(IDocumentStore docStore)
        {
            _docStore = docStore;
        }

        static void Main()
        {
            using (var docStore = new EmbeddableDocumentStore().Initialize())
            {
                new Program(docStore).Run();
                Console.ReadLine();
            }
        }

        private void Run()
        {
            //Seed();

            using (var db = _docStore.OpenSession())
            {
                var settings = db.Load<PodcastSettings>("podcastSettings");
                var casts = db.Query<Podcast>();
                foreach (var podcast in casts)
                {
                    podcast.DownloadLatest(new DirectoryInfo("download"));
                }
            }
        }

        private void Seed()
        {
            using (var db = _docStore.OpenSession())
            {
                db.Store(new PodcastSettings
                {
                    SplitLength = TimeSpan.FromSeconds(45),
                    TempoMultiplier = 1.25
                }, "podcastSettings");

                db.Store(new Podcast("http://feeds.feedburner.com/herdingcode?fmt=xml"), "podcasts/herdingcode");
                db.Store(new Podcast("http://feeds.feedburner.com/netRocksFullMp3Downloads?fmt=xml"), "podcasts/dotnetrocks");

                db.SaveChanges();
            }
            Console.WriteLine("Seeded");
        }
    }
}
