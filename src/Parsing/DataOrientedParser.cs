using System.Diagnostics;

using Scover.Option;
using Scover.UselessStatements.Lexing;
using Scover.UselessStatements.Parsing.DataOriented;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// Data-oriented parser
/// </summary>
public sealed class DataOrientedParser : Parser
{
    readonly Repeat<Node.Prog, Node.Stmt> _prog;
    readonly Action<ParserError> _reportError;

    /// <param name="reportError">The function to call to report an error (represented as a string)</param>
    public DataOrientedParser(Action<ParserError> reportError)
    {
        _reportError = reportError;
        var lparen = Terminal.Of(null, NoValue.Build, TokenType.LParen);
        var rparen = Terminal.Of(null, NoValue.Build, TokenType.RParen);
        var mul = Terminal.Of(null, TokenType.Mul);
        var div = Terminal.Of(null, TokenType.Div);
        var mod = Terminal.Of(null, TokenType.Mod);
        var plus = Terminal.Of(null, TokenType.Plus);
        var minus = Terminal.Of(null, TokenType.Minus);
        var semi = Terminal.Of("no-op", _ => new Node.Stmt.Nop(), TokenType.Semi);

        var number = new Terminal<Node.Stmt.Expr.Number>("number", value => new((decimal)value.NotNull()), TokenType.LitNumber);
        var bracedGroup = Concat.Of("braced group", (_, n, _) => n, lparen, (Step<Node.Stmt.Expr>?)null, rparen);
        var exprPrimary = Altern.Of("primary expression", [number, bracedGroup]);
        var exprMult = Concat.Of("multiplicative expression",
            (left, rights) => {
                Node.Stmt.Expr node = left;
                foreach (var (op, right) in rights.ValueOr([])) {
                    node = new Node.Stmt.Expr.Binary(node, op, right);
                }
                return node.Some();
            },
            exprPrimary,
            Repeat.Of(null,
                rights => rights,
                Concat.Of(null,
                    (op, right) => right.Map(r => (op, r)),
                    Altern.Of("multiplicative operator", [mul, div, mod]),
                    exprPrimary
                ))
        );
        var expr = Concat.Of("additive expression",
            (left, rights) => {
                Node.Stmt.Expr node = left;
                foreach (var (op, right) in rights.ValueOr([])) {
                    node = new Node.Stmt.Expr.Binary(node, op, right);
                }
                return node.Some();
            },
            exprMult,
            Repeat.Of(null,
                rights => rights,
                Concat.Of(null,
                    (op, right) => right.Map(r => (op, r)),
                    Altern.Of("additive operator", [plus, minus]),
                    exprMult
                ))
        );
        bracedGroup.SetStep2(expr);
        var stmt = Altern.Of<Node.Stmt>("statement", [semi, expr]);

        _prog = Repeat.Of("program", body => new Node.Prog(body), stmt, [TokenType.Eof]);
    }

    /// <inheritdoc />
    protected override Node.Prog Prog()
    {
        var r = _prog.Parse(Tokens, "useless-statements");
        Debug.Assert(r.HasValue);
        foreach (var error in r.Errors) {
            _reportError(error);
        }
        return r.Value;
    }
}
