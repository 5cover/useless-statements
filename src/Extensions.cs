using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

static class Extensions
{
    public static T NotNull<T>([NotNull] this T? t)
    {
        Debug.Assert(t is not null);
        return t;
    }

    public static void WriteLn(this TextWriter tw, int indent, object s)
    {
        for (int i = 0; i < indent * 2; ++i) {
            tw.Write(' ');
        }
        tw.WriteLine(s);
    }
}
