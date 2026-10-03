using System.Runtime.CompilerServices;

namespace Netprof;

internal class EventStream
{
    public readonly int Generation;
    public readonly int ThreadId;
    public readonly string? ThreadName;

    public EventStream? Next;

    public EventChunk Head { get; }
    private EventChunk _tail;

    public EventStream(int generation, int threadId, string? threadName)
    {
	Generation = generation;
        ThreadId = threadId;
        ThreadName = threadName;
        Head = _tail = new EventChunk();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(long timestamp, string name, EventKind kind, long spanId)
    {
	var tail = _tail;
	int index = tail.Count;

	if ((uint)index >= EventChunk.Capacity)
	{
	    var next = new EventChunk();
	    tail.Next = next;
            _tail = next;
            tail = next;
            index = 0;
	}

	tail.Events[index] = tail.Events[index] with { Timestamp = timestamp, Name = name, Kind = kind, SpanId = spanId };
	tail.Count = index + 1;

    }
}
