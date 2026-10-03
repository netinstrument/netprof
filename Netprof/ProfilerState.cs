namespace Netprof;

internal class ProfilerState
{
    public readonly int Generation;
    public readonly long StartTimestamp;
    public EventStream? Streams;

    public ProfilerState(int generation, long startTimestamp)
    {
        Generation = generation;
        StartTimestamp = startTimestamp;
    }

    public void AddEventStream(EventStream stream)
    {
        var head = Streams;

        while (true)
        {
            stream.Next = head;

            var observed = Interlocked.CompareExchange(ref Streams, stream, head);
            if (ReferenceEquals(observed, head))
            {
                return;
            }

            head = observed;
        }
    }
}
