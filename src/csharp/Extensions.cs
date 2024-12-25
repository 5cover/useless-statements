using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

static class Extensions
{
    public static T NotNull<T>([NotNull] this T? t)
    {
        Debug.Assert(t is not null);
        return t;
    }
}
