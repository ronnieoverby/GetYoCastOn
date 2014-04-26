using System;
using SomeDB;

namespace GetYoCastOn
{
    public class PodcastSettings : IDocument
    {
        public TimeSpan SplitLength { get; set; }
        public double TempoMultiplier { get; set; }
        public string Id { get; set; }
    }
}