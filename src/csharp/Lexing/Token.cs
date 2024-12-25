namespace Scover.UselessStatements.Lexing;

readonly record struct Token(Range Extent, TokenType Type, object? Value = null);

enum TokenType
{
    Plus,
    Div,
    Mod,
    Minus,
    Mul,
    Semi,
    LitNumber,
}

