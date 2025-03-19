namespace Scover.UselessStatements.Lexing;

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
