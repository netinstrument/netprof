using System.Runtime.CompilerServices;

namespace Netprof;

public ref struct Zone
{
    private readonly int _generation;
    private readonly string _name;

    internal Zone(int generation, string name)
    {
         _generation = generation;
         _name = name;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Profiler.EndZone(_generation, _name);
    }
}
