using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Commerce.Api;

public static class MeterCounterCache
{
    private static readonly ConcurrentDictionary<string, Instrument> _cache = new ();

    public static Counter<T>? GetCounter<T>(this Meter meter, string counterName) where T : struct
    {
        return _cache.GetOrAdd(counterName, s => meter.CreateCounter<T>(s)) as Counter<T>;
    }
}
