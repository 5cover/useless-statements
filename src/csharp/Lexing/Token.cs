namespace Scover.UselessStatements.Lexing;

readonly record struct Token(Range Extent, TokenType Type, object? Value = null);

enum TokenType
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

