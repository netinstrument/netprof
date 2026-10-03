using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Netprof;

/// <summary>
/// Records profiling events in separate per-thread streams and collects them into captures.
/// </summary>
public static class Profiler
{
    private static ProfilerState _state = new(0, Stopwatch.GetTimestamp());
    private static long _nextSpanId;

    [ThreadStatic]
    private static EventStream? _stream;

    /// <summary>
    /// Starts a new recording generation and returns a capture of the previous generation.
    /// </summary>
    public static ProfileCapture Capture()
    {
        while (true)
        {
            var state = Volatile.Read(ref _state);
            var next = new ProfilerState(unchecked(state.Generation + 1), Stopwatch.GetTimestamp());

            if (ReferenceEquals(Interlocked.CompareExchange(ref _state, next, state), state))
            {
                return new ProfileCapture(state, Environment.ProcessId, Stopwatch.Frequency);
            }
        }
    }

    /// <summary>
    /// Records the beginning of a synchronous event on the current thread.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void BeginEvent(string name)
    {
        var state = Volatile.Read(ref _state);
        WriteEvent(state, name, EventKind.Begin, 0);
    }

    /// <summary>
    /// Records the end of a synchronous event on the current thread.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void EndEvent(string name)
    {
        var state = Volatile.Read(ref _state);
        WriteEvent(state, name, EventKind.End, 0);
    }

    /// <summary>
    /// Records the beginning of an asynchronous event that can end on another thread.
    /// </summary>
    /// <returns>
    /// A token identifying the event and its recording generation.
    /// Pass this token to <see cref="EndAsyncEvent"/> to end the event.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static AsyncEventToken BeginAsyncEvent(string name)
    {
        var state = Volatile.Read(ref _state);
        var spanId = Interlocked.Increment(ref _nextSpanId);
        WriteEvent(state, name, EventKind.AsyncBegin, spanId);
        return new AsyncEventToken(state.Generation, name, spanId);
    }

    /// <summary>
    /// Records the end of the asynchronous event identified by the specified token.
    /// </summary>
    /// <remarks>
    /// This method can be called on a different thread from the beginning event.
    /// If the token belongs to a previous recording generation, no event is recorded.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static void EndAsyncEvent(AsyncEventToken token)
    {
        var state = Volatile.Read(ref _state);
        if (state.Generation != token.Generation)
        {
            return;
        }

        WriteEvent(state, token.Name, EventKind.AsyncEnd, token.SpanId);
    }

    /// <summary>
    /// Records the beginning of a synchronous event and returns a zone for ending it.
    /// </summary>
    /// <returns>A zone associated with the event's name and recording generation.</returns>
    /// <remarks>
    /// End the zone on the same stack frame on which it was entered.
    /// If the recording generation changes before the zone ends, its end event is ignored.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static Zone EnterZone(string name)
    {
        var state = Volatile.Read(ref _state);
        WriteEvent(state, name, EventKind.Begin, 0);
        return new Zone(state.Generation, name);
    }

    /// <summary>
    /// Records the beginning of an asynchronous event and returns a zone for ending it.
    /// </summary>
    /// <returns>A zone containing the token that identifies the asynchronous event.</returns>
    /// <remarks>
    /// The zone can end on another thread.
    /// If the recording generation changes before the zone ends, its end event is ignored.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static AsyncZone EnterAsyncZone(string name)
    {
        return new AsyncZone(BeginAsyncEvent(name));
    }

    /// <summary>
    /// Records the end of a synchronous zone if its recording generation is still current.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal static void EndZone(int generation, string name)
    {
        var state = Volatile.Read(ref _state);
        if (state.Generation != generation)
        {
            return;
        }

        WriteEvent(state, name, EventKind.End, 0);
    }

    /// <summary>
    /// Appends a timestamped event to the current thread's event stream for the specified state,
    /// creating and registering a new stream for the current thread when necessary.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static void WriteEvent(ProfilerState state, string name, EventKind kind, long spanId)
    {
        var stream = _stream;
        if (stream is null || stream.Generation != state.Generation)
        {
            stream = new EventStream(state.Generation, Environment.CurrentManagedThreadId, Thread.CurrentThread.Name);
            state.AddEventStream(stream);
            _stream = stream;
        }

        stream.Append(Stopwatch.GetTimestamp(), name, kind, spanId);
    }
}
