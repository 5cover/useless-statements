using System.Diagnostics;
using Scover.UselessStatements;
using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

static class AstPrinter
{
    public static void PrettyPrint(this Node.Prog prog)
    {
        Console.WriteLine("Prog");
        foreach (var stmt in prog.Body) {
            stmt.PrettyPrint(1);
        }
    }

    static void PrettyPrint(this Node.Stmt stmt, int lvl)
    {
        switch (stmt) {
        case Node.Stmt.Nop: Console.Out.WriteLn(lvl, ';'); break;
        case Node.Stmt.Expr e: e.PrettyPrint(lvl); break;
        default:
            throw new UnreachableException();
        }
    }

    static void PrettyPrint(this Node.Stmt.Expr expr, int lvl)
    {
        switch (expr) {
        case Node.Stmt.Expr.Number n: Console.Out.WriteLn(lvl, n.Value); break;
        case Node.Stmt.Expr.Binary b:
            Console.Out.WriteLn(lvl, b.Op.GetOperator());
            b.Lhs.PrettyPrint(lvl + 1);
            b.Rhs.PrettyPrint(lvl + 1);
            break;
        default:
            throw new UnreachableException();
        }
    }

    static char GetOperator(this TokenType t) => t switch {
        Div => '/',
        Minus => '-',
        Mod => '%',
        Mul => '*',
        Plus => '+',
        _ => throw new UnreachableException(),
    };
}
