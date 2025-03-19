using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements;

public readonly record struct ParserError(int Index, string Subject, IReadOnlySet<TokenType> Expected)
{
    public bool Equals(ParserError other) => other.Index == Index && other.Expected.SetEquals(Expected) && other.Subject == Subject;
    public override int GetHashCode() => HashCode.Combine(Index, Expected, Subject);
}

public readonly record struct LexerError(int Index, string Message);
