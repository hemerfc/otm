using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Otm.Server.OTel;

public class OTelContextManager
{
    public static ActivityContext Extract(IDictionary<string, object> headers)
    {

        var parentContext = Propagators.DefaultTextMapPropagator.Extract(
            default,
            headers,
            (headers, key) =>
            {
                if (headers.TryGetValue(key, out var value) && value is byte[] bytes)
                {
                    return [Encoding.UTF8.GetString(bytes)];
                }
                return [];
            });

        return parentContext.ActivityContext;
    }

    public static void Inject(IDictionary<string, object> headers)
    {
        if (Activity.Current != null)
        {
            Propagators.DefaultTextMapPropagator.Inject(
                new PropagationContext(Activity.Current.Context, Baggage.Current),
                headers,
                (headers, key, value) =>
                {
                    headers[key] = Encoding.UTF8.GetBytes(value);
                });
        }
    }
}
