using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements.Parsing;

public abstract class Parser(Action<ParserError> reportError)
{
    protected IReadOnlyList<Token> Tokens { get; private set; } = [];

    /// <summary>
    /// Current token index
    /// </summary>
    protected int I { get; set; }

    protected bool IsAtEnd => I >= Tokens.Count || Tokens[I].Type == TokenType.Eof;

    public Node.Prog Parse(IReadOnlyList<Token> tokens)
    {
        Tokens = tokens;
        I = 0;
        return Prog();
    }

    protected abstract Node.Prog Prog();

    protected void Error(string subject, IEnumerable<TokenType> expected) => reportError(new(I, subject, expected.ToHashSet()));
}
