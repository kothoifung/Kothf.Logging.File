using System.Text;

namespace Kothf.Logging.File.Internal;

internal static class StringBuilderCache
{
    [ThreadStatic]
    private static StringBuilder? _cached;

    public static StringBuilder Acquire(int capacity = 360)
    {
        var sb = _cached;
        if (sb is null || sb.Capacity < capacity)
        {
            return new StringBuilder(capacity);
        }

        _cached = null;
        sb.Clear();
        return sb;
    }

    public static void Release(StringBuilder sb)
    {
        if (sb.Capacity <= 4096)
        {
            _cached = sb;
        }
    }
}
