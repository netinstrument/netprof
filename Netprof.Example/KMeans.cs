using System.Runtime.CompilerServices;
using Netprof;

namespace Netprof.Example;

/// <summary>
/// K-means clustering algorithm.
/// </summary>
public static class KMeans
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Point[] Cluster(int pointCount, int clusterCount, int iterations, int seed)
    {
	var random = new Random(seed);

	var points = new Point[pointCount];
	var centers = new Point[clusterCount];
	var sums = new Point[clusterCount];
	var counts = new int[clusterCount];

	// NOTE(alex): Fill initial points with random data.
	for (var i = 0; i < pointCount; i += 1)
	{
	    points[i] = new Point(random.NextDouble() * 1000, random.NextDouble() * 1000, random.NextDouble() * 1000);
	}

	// NOTE(alex): The generated points are random, so use the first K as initial centers.
	Array.Copy(points, centers, clusterCount);

	for (var iteration = 0; iteration < iterations; iteration += 1)
	{
	    Profiler.BeginEvent("Iteration");

	    Profiler.BeginEvent("Clear");
	    Array.Clear(sums, 0, sums.Length);
	    Array.Clear(counts, 0, counts.Length);
	    Profiler.EndEvent("Clear");

	    Profiler.BeginEvent("FindNearestClusters");
	    foreach (var point in points)
	    {
		// NOTE(alex): Find the nearest cluster to each point.
		var nearest = 0;
		var minDistanceSquared = double.MaxValue;

		for (var cluster = 0; cluster < clusterCount; cluster += 1)
		{
		    var dx = point.X - centers[cluster].X;
		    var dy = point.Y - centers[cluster].Y;
		    var dz = point.Z - centers[cluster].Z;
		    var distanceSquared = dx * dx + dy * dy + dz * dz;

		    if (distanceSquared < minDistanceSquared)
		    {
			minDistanceSquared = distanceSquared;
			nearest = cluster;
		    }
		}

		// NOTE(alex): Aggregate sum and count into the nearest cluster.
		sums[nearest] += point;
		counts[nearest] += 1;
	    }
	    Profiler.EndEvent("FindNearestClusters");

	    // NOTE(alex): Move each nonempty cluster's center to its mean.
	    Profiler.BeginEvent("AdjustCenters");
	    for (var cluster = 0; cluster < clusterCount; cluster += 1)
	    {
		var count = counts[cluster];
		if (count == 0)
		{
		    continue;
		}

		centers[cluster] = new Point(sums[cluster].X / count, sums[cluster].Y / count, sums[cluster].Z / count);
	    }
	    Profiler.EndEvent("AdjustCenters");

	    Profiler.EndEvent("Iteration");
	}

	return centers;
    }
}
