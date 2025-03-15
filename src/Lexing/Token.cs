using System.Diagnostics;

namespace Scover.UselessStatements.Lexing;

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

public readonly record struct Token(FixedRange Extent, TokenType Type, object? Value = null);

public enum TokenType
{
    // The Eof token allow us to not worry about the size of our token streams.
    // It makes some things easier for knowing when we're at the end and things like that.
    Eof,

    Div,
    Minus,
    Mod,
    Mul,
    Plus,
    Semi,

    LParen,
    RParen,

    LitNumber,
}

