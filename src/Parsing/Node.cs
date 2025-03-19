using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements.Parsing;

public interface Node
{
    interface Stmt : Node
    {
        interface Expr : Stmt
        {
            sealed class Binary(Expr lhs, TokenType op, Expr rhs) : Expr
            {
                public Expr Lhs { get; } = lhs;
                public TokenType Op { get; } = op;
                public Expr Rhs { get; } = rhs;
            }

            sealed class Number(decimal value) : Expr
            {
                public decimal Value { get; } = value;
            }
        }

        sealed class Nop : Stmt;
    }

    sealed class Prog(IReadOnlyList<Stmt> body) : Node
    {
        public IReadOnlyList<Stmt> Body { get; } = body;
    }
}
