using System.Diagnostics.Metrics;

namespace SEN_T_PAZAR.Services;

public static class ApiObservability
{
    private static readonly Meter Meter = new("SENTPAZAR.Api", "1.0.0");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("api.requests.total");
    private static readonly Counter<long> ClientErrorCounter = Meter.CreateCounter<long>("api.requests.4xx");
    private static readonly Counter<long> ServerErrorCounter = Meter.CreateCounter<long>("api.requests.5xx");

    public static void Record(string route, int statusCode)
    {
        var tags = new KeyValuePair<string, object?>[]
        {
            new("route", route),
            new("status", statusCode)
        };

        RequestCounter.Add(1, tags);

        if (statusCode >= 400 && statusCode < 500)
        {
            ClientErrorCounter.Add(1, tags);
        }

        if (statusCode >= 500)
        {
            ServerErrorCounter.Add(1, tags);
        }
    }
}
