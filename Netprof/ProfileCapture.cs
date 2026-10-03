using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Netprof;

public class ProfileCapture
{
    private readonly ProfilerState _state;
    private readonly int _processId;
    private readonly long _timestampFrequency;

    internal ProfileCapture(ProfilerState state, int processId, long timestampFrequency)
    {
	_state = state;
	_processId = processId;
	_timestampFrequency = timestampFrequency;
    }

    public void ExportChromeTrace(string path)
    {
	ArgumentException.ThrowIfNullOrEmpty(path);

	using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 64 * 1024, options: FileOptions.SequentialScan);

	var options = new JsonWriterOptions { Indented = false };
	using var writer = new Utf8JsonWriter(file, options);

	writer.WriteStartObject();
        writer.WriteStartArray("traceEvents");

	var streams = _state.Streams;
	for (var stream = streams; stream is not null; stream = stream.Next)
	{
	    if (stream.ThreadName is not null)
	    {
		WriteThreadNameMetadata(writer, stream);
	    }

	    for (var chunk = stream.Head; chunk is not null; chunk = chunk.Next)
	    {
		for (var i = 0; i < chunk.Count; i += 1)
		{
		    WriteTraceEvent(writer, stream.ThreadId, chunk.Events[i]);
		}
	    }
	}

	writer.WriteEndArray();
	writer.WriteString("displayTimeUnit", "ms");
	writer.WriteEndObject();
	writer.Flush();
    }

    private void WriteThreadNameMetadata(Utf8JsonWriter writer, EventStream stream)
    {
        writer.WriteStartObject();
        writer.WriteString("name", "thread_name");
        writer.WriteString("ph", "M");
        writer.WriteNumber("pid", _processId);
        writer.WriteNumber("tid", stream.ThreadId);
        writer.WriteStartObject("args");
        writer.WriteString("name", stream.ThreadName);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private void WriteTraceEvent(Utf8JsonWriter writer, int threadId, in Event record)
    {
	writer.WriteStartObject();
	writer.WriteString("name", record.Name);
	writer.WriteString("ph", GetPhase(record.Kind));
	writer.WriteNumber("ts", ToMicroseconds(record.Timestamp - _state.StartTimestamp, _timestampFrequency));
	writer.WriteNumber("pid", _processId);
        writer.WriteNumber("tid", threadId);

	if (record.Kind is EventKind.AsyncBegin or EventKind.AsyncEnd)
	{
	    writer.WriteString("id", record.SpanId.ToString("x16", CultureInfo.InvariantCulture));
	}

	writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetPhase(EventKind kind)
    {
        return kind switch
        {
            EventKind.Begin => "B",
            EventKind.End => "E",
            EventKind.AsyncBegin => "b",
            EventKind.AsyncEnd => "e",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static long ToMicroseconds(long timestampDelta, long frequency)
    {
        Int128 scaled = (Int128)timestampDelta * 1_000_000;
        return (long)(scaled / frequency);
    }
}
