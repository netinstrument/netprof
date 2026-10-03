namespace Netprof.Example;

/// <summary>
/// A point in 3-dimensional space.
/// </summary>
public record struct Point(double X, double Y, double Z)
{
    public static Point operator +(Point a, Point b)
    {
	return new Point(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }
}
