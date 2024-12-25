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

    Node.Stmt.Expr? Expr() => ParseExprBinaryLeftAssociative(ExprMult, [Plus, Minus]);

    Node.Stmt.Expr? ExprMult() => ParseExprBinaryLeftAssociative(ExprPrimary, [Mul, Div, Mod]);

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

    Node.Stmt.Expr? ParseExprBinaryLeftAssociative<T>(Func<T?> operand, TokenType[] operators) where T : Node.Stmt.Expr
    {
        Node.Stmt.Expr? expr = operand(); if (expr is null) return null;
        while (Match(operators, out var op)) {
            var rhs = operand(); if (rhs is null) return null;
            expr = new Node.Stmt.Expr.Binary(expr, op, rhs);
        }
        return expr;
    }
}
