using System.Runtime.CompilerServices;
using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements;

sealed class PrimitiveParser(IReadOnlyList<Token> tokens)
{
    readonly IReadOnlyList<Token> _tokens = tokens;
    int _i;
    public Node.Prog Parse() => Prog();

    Node.Prog Prog()
    {
        using var _ = Enter();
        List<Node.Stmt> body = [];
        Node.Stmt? s;
        while (!IsAtEnd && (s = Stmt()) is not null) {
            body.Add(s);
        }
        return new(body);
    }

    Node.Stmt? Stmt()
    {
        using var _ = Enter();
        return Match(Semi) ? new Node.Stmt.Nop() : Expr();
    }

    Node.Stmt.Expr? Expr()
    {
        using var _ = Enter();
        return ParseExprBinaryLeftAssociative(ExprMult, [Plus, Minus]);
    }

    Node.Stmt.Expr? ExprMult()
    {
        using var _ = Enter();
        return ParseExprBinaryLeftAssociative(ExprPrimary, [Mul, Div, Mod]);
    }

    Node.Stmt.Expr? ExprPrimary()
    {
        using var _ = Enter();
        if (Match(LitNumber, out var value)) {
            return new Node.Stmt.Expr.Number((decimal)value.NotNull());
        } else if (Match(LParen)) {
            var expr = Expr();
            // The right parenthesis doesn't bring any new information.
            // The parser would still work if we did not require it, but it would lead to confusion and ambiguity on the user's side.
            // - `(1+2)*3` yields 9
            // - `(1+2*3` yields 6 but it is almost certain the user meant the first. not requiring a RParen means we silently change the meaning of the expression instead of causing a syntax error.
            if (!Match(RParen)) return null;
            return expr;
        }

        return null;
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

    int _lvl;

    Deferree Enter([CallerMemberName] string caller = "")
    {
        Console.Out.WriteLn(_lvl++, $"enter {caller}");
        return new Deferree(this, caller);
    }

    readonly struct Deferree(PrimitiveParser owner, string caller) : IDisposable
    {
        public void Dispose() => Console.Out.WriteLn(--owner._lvl, $"leave {caller}");
    }
}
