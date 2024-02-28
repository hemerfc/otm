using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Otm.Server.OpenTelemetry;

public class TelemetryConstants
{
    /// <summary>
    /// The name of the <see cref="ActivitySource"/> that is going to produce our traces and
    /// the <see cref="Meter"/> that is going to produce our metrics.
    /// </summary>
    public const string MyAppSource = "OTM";
    public static string ServiceName = "OTM";
    public static string ServiceNameSpace = "OTM";
    public static string ServiceVersion= "1";

    public static readonly ActivitySource DemoTracer = new ActivitySource(MyAppSource);

    public static readonly Meter DemoMeter = new Meter(MyAppSource);

}