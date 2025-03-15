using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Scover.UselessStatements;

public enum ErrorVerb
{
    Insert,
    Replace,
    Remove,
}

public readonly record struct SyntaxError(int Index, ErrorVerb Verb, string Subject);

public readonly record struct FixedRange
{
    public FixedRange(int start, int end)
    {
        Debug.Assert(start <= end);
        (Start, End) = (start, end);
    }

    public static FixedRange Of(int start, int length) => new(start, start + length);

    public static explicit operator Range(FixedRange fixedRange) => fixedRange.Start..fixedRange.End;

    public int Start { get; }
    public int End { get; }
    public int Length => End - Start;
}

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
