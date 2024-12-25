namespace Scover.UselessStatements.Lexing;

readonly record struct Token(Range Extent, TokenType Type, object? Value = null);

enum TokenType
{
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

