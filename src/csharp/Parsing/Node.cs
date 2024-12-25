using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements;

interface Node
{
    sealed class Prog(IReadOnlyList<Stmt> body) : Node
    {
        internal IReadOnlyList<Stmt> Body { get; } = body;
    }
    interface Stmt : Node
    {
        interface Expr : Stmt
        {
            sealed class Binary(Expr lhs, TokenType op, Expr rhs) : Expr
            {
                internal Expr Lhs { get; } = lhs;
                internal TokenType Op { get; } = op;
                internal Expr Rhs { get; } = rhs;
            }
            sealed class Number(decimal value) : Expr
            {
                internal decimal Value { get; } = value;
            }
        }
        sealed class Nop : Stmt
        {
        }
    }
}
