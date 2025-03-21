using System.Diagnostics;

namespace Scover.UselessStatements;

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
