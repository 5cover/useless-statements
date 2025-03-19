using Scover.UselessStatements.Lexing;

using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// A primitive parser. Returns <see langword="null"/> and calls that error handling.
/// </summary>
public sealed class PrimitiveParser : Parser
{
    protected override Node.Prog Prog()
    {
        List<Node.Stmt> body = [];
        while (!IsAtEnd) {
            var s = Stmt();
            if (s is not null) body.Add(s);
        }
        return new(body);
    }

    Node.Stmt.Expr? Expr() => ParseExprBinaryLeftAssociative(ExprMult, [Plus, Minus]);

    Node.Stmt.Expr? ExprMult() => ParseExprBinaryLeftAssociative(ExprPrimary, [Mul, Div, Mod]);

    Node.Stmt.Expr? ExprPrimary()
    {
        if (Match(LitNumber, out var value)) {
            return new Node.Stmt.Expr.Number((decimal)value.NotNull());
        } else if (Match(LParen)) {
            var expr = Expr();
            // The right parenthesis doesn't bring any new information.
            // The parser would still work if we did not require it, but it would lead to confusion and ambiguity on the user's side.
            // - `(1+2)*3` yields 9
            // - `(1+2*3` yields 6 but it is almost certain the user meant the first. not requiring a RParen means we silently change the meaning of the expression instead of causing a syntax error.
            return !Match(RParen) ? null : expr;
        }

        return null;
    }

    bool Match(TokenType expected, out object? value)
    {
        if (IsAtEnd || expected != Tokens[I].Type) {
            value = null;
            return false;
        }
        value = Tokens[I++].Value;
        return true;
    }

    bool Match(TokenType[] expected, out TokenType choosen)
    {
        if (IsAtEnd || !expected.Contains(Tokens[I].Type)) {
            choosen = default;
            return false;
        }
        choosen = Tokens[I++].Type;
        return true;
    }

    bool Match(TokenType expected)
    {
        if (IsAtEnd || expected != Tokens[I].Type) {
            return false;
        }
        I++;
        return true;
    }

    Node.Stmt.Expr? ParseExprBinaryLeftAssociative(Func<Node.Stmt.Expr?> operand, TokenType[] operators)
    {
        Node.Stmt.Expr? expr = operand(); if (expr is null) return null;
        while (Match(operators, out var op)) {
            var rhs = operand(); if (rhs is null) return null;
            expr = new Node.Stmt.Expr.Binary(expr, op, rhs);
        }
        return expr;
    }

    Node.Stmt? Stmt() => Match(Semi) ? new Node.Stmt.Nop() : Expr();
}
