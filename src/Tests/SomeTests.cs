using GetYoCastOn;
using NUnit.Framework;

namespace Tests
{
    public class SomeTests
    {
        [Test]
        public void Can_Get_Title_From_Feed()
        {
            var podcast = new Podcast("http://feeds.feedburner.com/herdingcode?fmt=xml");
            Assert.AreEqual("Herding Code", podcast.Title);
        }
    }
}
