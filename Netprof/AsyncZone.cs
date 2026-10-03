using System.Runtime.CompilerServices;

namespace Netprof;

public struct AsyncZone
{
    private readonly AsyncEventToken _token;

    internal AsyncZone(AsyncEventToken token)
    {
	_token = token;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
	Profiler.EndAsyncEvent(_token);
    }
}
