using System;
using System.Collections.Concurrent;
using System.Text;

namespace SomeDB
{
    public class Stats : ConcurrentDictionary<string, Stats.Stat>
    {
        public class Stat
        {
            public string Name { get; private set; }
            public int Iterations { get; private set; }
            public TimeSpan TotalDuration { get; private set; }

            public TimeSpan AverageDuration
            {
                get { return TimeSpan.FromTicks(TotalDuration.Ticks/Iterations); }
            }

            public Stat(string name, TimeSpan elapsed)
            {
                Name = name;
                TotalDuration += elapsed;
                Iterations = 1;
            }

            internal Stat Record(TimeSpan elapsed)
            {
                Iterations++;
                TotalDuration += elapsed;
                return this;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var stat in Values)
            {
                sb.AppendFormat("{0} :: Iters: {3:n0}; Total: {1}; Avg: {2}", stat.Name, stat.TotalDuration,
                    stat.AverageDuration, stat.Iterations)
                    .AppendLine();
            }
            return sb.ToString();
        }
    }
}