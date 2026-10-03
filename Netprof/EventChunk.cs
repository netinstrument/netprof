using System.Runtime.CompilerServices;

namespace Netprof;

internal class EventChunk
{
    public const int Capacity = 1024;

    public EventChunk? Next;
    public int Count;
    public EventArray Events;

    [InlineArray(Capacity)]
    public struct EventArray
    {
        private Event _element0;
    }
}
