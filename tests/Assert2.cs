using Scover.UselessStatements;
using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatementsTests;

static class Assert2
{
    public static async Task Number(Node.Stmt? stmt, decimal value)
     => await Assert.That((await Assert.That(stmt).IsTypeOf<Node.Stmt.Expr.Number>())?.Value).IsEqualTo(value);

    public static async Task<Node.Stmt.Expr.Binary> Binary(Node.Stmt? stmt, TokenType op)
    {
        var e = await Assert.That(stmt).IsTypeOf<Node.Stmt.Expr.Binary>();
        await Assert.That(e!.Op).IsEqualTo(op);
        return e;
    }
}
