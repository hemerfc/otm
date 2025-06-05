using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Otm.Server.OTel.Activities
{
    public class PalantirActivity
    {
        public const string Name = "Palantir.Activity";

        public static Meter Meter { get; } = new Meter(Name);
        public static Counter<int> ConsumedMessages { get; } = Meter.CreateCounter<int>("palantir.messages.consumed");
        public static Counter<int> PublishedMessages { get; } = Meter.CreateCounter<int>("palantir.messages.published");
        public static ActivitySource ActivitySource { get; } = new ActivitySource(Name);
    }
}