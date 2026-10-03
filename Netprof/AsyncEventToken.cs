namespace Netprof;

public struct AsyncEventToken
{
    internal AsyncEventToken(int generation, string name, long spanId)
    {
	Generation = generation;
	Name = name;
	SpanId = spanId;
    }

    internal int Generation { get; }

    internal string Name { get; }

    internal long SpanId { get; }
}
