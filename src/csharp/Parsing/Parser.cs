using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements.Parsing;

abstract class Parser
{
    protected IReadOnlyList<Token> Tokens { get; private set; } = [];
    protected int _i;

    public Node.Prog Parse(IReadOnlyList<Token> tokens)
    {
        Tokens = tokens;
        _i = 0;
        return Prog();
    }

    protected abstract Node.Prog Prog();
}