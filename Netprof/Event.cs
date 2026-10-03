namespace Netprof;

internal struct Event
{
    public long Timestamp;
    public long SpanId;
    public string? Name;
    public EventKind Kind;
}
