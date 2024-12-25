using System.Diagnostics;
using Scover.UselessStatements;

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
        case Node.Stmt.Nop: WriteLn(lvl, ';'); break;
        case Node.Stmt.Expr e: e.PrettyPrint(lvl); break;
        default:
            throw new UnreachableException();
        }
    }

    static void PrettyPrint(this Node.Stmt.Expr expr, int lvl)
    {
        switch (expr) {
        case Node.Stmt.Expr.Number n: WriteLn(lvl, n.Value); break;
        case Node.Stmt.Expr.Binary b:
            WriteLn(lvl, $"Binary {b.Op}");
            b.Lhs.PrettyPrint(lvl + 1);
            b.Rhs.PrettyPrint(lvl + 1);
            break;
        default:
            throw new UnreachableException();
        }
    }

    static void WriteLn(int indent, object s)
    {
        for (int i = 0; i < indent * 4; ++i) {
            Console.Write(' ');
        }
        Console.WriteLine(s);
    }
}
