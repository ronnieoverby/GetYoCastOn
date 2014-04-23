using System;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;

namespace GetYoCastOn
{
    public class Podcast
    {
        public Podcast(string url)
        {
            FeedUrl = url;
            Title = GetTitle(FeedUri);
        }

        private static string GetTitle(Uri feedUri)
        {
            var feed = GetFeed(feedUri);
            return feed.Title.Text;
        }

        private static SyndicationFeed GetFeed(Uri feedUri)
        {
            using (var stream = new WebClient().OpenRead(feedUri))
            using (var xml = new XmlTextReader(stream))
            {
                var feed = SyndicationFeed.Load(xml);
                return feed;
            }
        }

        public Podcast()
        {
            
        }

        public string Id { get; set; }
        public string FeedUrl { get; set; }

        public Uri FeedUri
        {
            get
            {
                if (FeedUrl != null)
                    return new Uri(FeedUrl);
                return null;
            }
        }

        public string Title { get; set; }
        public PodcastSettings Settings { get; set; }

        public void DownloadLatest(DirectoryInfo directoryInfo)
        {
            var feed = GetFeed(FeedUri);
            var latest = feed.Items.OrderBy(x => x.PublishDate).LastOrDefault();

            if (latest == null)
                return;

            var link =
                latest.Links.OrderByDescending(ScoreLinkForAudio).FirstOrDefault();

            if (link == null)
                return;

            var dest = new FileInfo(Path.Combine(directoryInfo.FullName, link.Uri.LocalPath));
            dest.Directory.Create();
            

            using (var read = new WebClient().OpenRead(link.Uri))
            using (var write = File.OpenWrite(dest.FullName))
                read.CopyTo(write);
        }

        private static int ScoreLinkForAudio(SyndicationLink link)
        {
            if (link == null) throw new ArgumentNullException("link");

            var score = 0;

            if (link.Length > 0)
                score++;

            const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            if (link.RelationshipType != null && link.RelationshipType.Equals("enclosure", ignoreCase))
                score++;

            var exts = new[] {"mp3", "ogg", "wav"};
            if (exts.Any(x => link.Uri.LocalPath.EndsWith(x, ignoreCase)))
                score++;

            if (link.MediaType != null && link.MediaType.IndexOf("audio", ignoreCase) != -1)
                score++;

            return score;
        }
    }
}