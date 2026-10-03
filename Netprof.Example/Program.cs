using Netprof;
using Netprof.Example;

using (Profiler.EnterZone(nameof(KMeans)))
{
    var centers = KMeans.Cluster(pointCount: 200_000, clusterCount: 32, iterations: 50, seed: 42);
}

var capture = Profiler.Capture();
capture.ExportChromeTrace("trace.json");
