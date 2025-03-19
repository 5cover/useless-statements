using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements.Parsing;

public abstract class Parser
{
    /// <summary>
    /// Current token index
    /// </summary>
    protected int I { get; set; }

    protected bool IsAtEnd => I >= Tokens.Length || Tokens[I].Type == TokenType.Eof;
    protected Token[] Tokens { get; private set; } = [];

    public Node.Prog Parse(Token[] tokens)
    {
        Tokens = tokens;
        I = 0;
        return Prog();
    }

    protected abstract Node.Prog Prog();
}
