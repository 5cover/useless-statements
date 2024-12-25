using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements;

sealed class Parser(IReadOnlyList<Token> tokens)
{
    readonly IReadOnlyList<Token> _tokens = tokens;
    int _i;
    public Node.Prog Parse()
    {
        List<Node.Stmt> body = [];
        Node.Stmt? s;
        while (!IsAtEnd && (s = Stmt()) is not null) {
            body.Add(s);
        }
        return new(body);
    }

    Node.Stmt? Stmt() => Match(Semi) ? new Node.Stmt.Nop() : Expr();

    Node.Stmt.Expr? Expr()
    {
        Node.Stmt.Expr? expr = ExprMult(); if (expr is null) return null;
        while (Match([Plus, Minus], out var op)) {
            var rhs = ExprMult(); if (rhs is null) return null;
            expr = new Node.Stmt.Expr.Binary(expr, op, rhs);
        }
        return expr;
    }

    Node.Stmt.Expr? ExprMult()
    {
        Node.Stmt.Expr? expr = ExprPrimary(); if (expr is null) return null;
        while (Match([Mul, Div, Mod], out var op)) {
            var rhs = ExprPrimary(); if (rhs is null) return null;
            expr = new Node.Stmt.Expr.Binary(expr, op, rhs);
        }
        return expr;
    }

    Node.Stmt.Expr.Number? ExprPrimary()
    {
        if (!Match(LitNumber, out var value)) return null;
        return new((decimal)value.NotNull());
    }

    bool Match(TokenType expected, out object? value)
    {
        if (IsAtEnd || expected != _tokens[_i].Type) {
            value = null;
            return false;
        }
        value = _tokens[_i++].Value;
        return true;
    }

    bool Match(TokenType[] expected, out TokenType choosen)
    {
        if (IsAtEnd || !expected.Contains(_tokens[_i].Type)) {
            choosen = default;
            return false;
        }
        choosen = _tokens[_i++].Type;
        return true;
    }

    bool Match(TokenType expected)
    {
        if (IsAtEnd || expected != _tokens[_i].Type) {
            return false;
        }
        _i++;
        return true;
    }

    bool IsAtEnd => _i >= _tokens.Count;
}
