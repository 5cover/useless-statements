using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// An helpful parser that produces error messages and returns null on error.
/// </summary>
/// <param name="tokens">The tokens to parse</param>
/// <param name="reportError">The function to call to report an error (represented as a string)</param>
/// <remarks>
/// The helpful parser works by reporting errors when it fails to parse a terminal.
/// When a non-terminal fails (a parsing function returns <see langword="null"/>), it doesn't report an error, as the error has already been reported for the particular terminal that failed.
/// </remarks>
sealed class HelpfulParser(HelpfulParser.ErrorReporter reportError) : Parser
{
    internal delegate void ErrorReporter(int tokenIndex, string message);
    readonly ErrorReporter _reportError = reportError;

    protected override Node.Prog Prog()
    {
        List<Node.Stmt> body = [];
        while (!IsAtEnd) {
            int iStart = _i;
            var s = Stmt();
            if (s is not null) body.Add(s);
            // A Very Primitive Sychronization
            // but it prevents infinite loops if nothing was parsed.
            // Check if we read something before incrementing - otherwise we risk skipping valid tokens.
            // Example : "(5+;". This prog fails in expr on ';', but ';' should still be parsed as a valid nop.
            // Trade-off : duplicate errors. Take "(5+)". The prog fails in expr on ')'. The prog tries again and fails again with the same error
            // solution : disallow reporting multiple errors on the same token (see Error)
            else if (iStart == _i) _i++;
        }
        return new(body);
    }

    Node.Stmt? Stmt() => Match(Semi) ? new Node.Stmt.Nop() : Expr();

    Node.Stmt.Expr? Expr() => ParseExprBinaryLeftAssociative(ExprMult, [Plus, Minus]);

    Node.Stmt.Expr? ExprMult() => ParseExprBinaryLeftAssociative(ExprPrimary, [Mul, Div, Mod]);

    Node.Stmt.Expr? ExprPrimary()
    {
        if (Match(LitNumber, out var value)) {
            return new Node.Stmt.Expr.Number((decimal)value.NotNull());
        } else if (Match(LParen)) {
            var expr = Expr(); if (expr is null) return null;
            if (Match(RParen)) return expr;
            else Error("Expected ')'");
        } else {
            Error("Expected number or '('");
        }

        return null;
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

    bool Match(TokenType expected, out object? value)
    {
        if (IsAtEnd || expected != Tokens[_i].Type) {
            value = null;
            return false;
        }
        value = Tokens[_i++].Value;
        return true;
    }

    bool Match(TokenType[] expected, out TokenType choosen)
    {
        if (IsAtEnd || !expected.Contains(Tokens[_i].Type)) {
            choosen = default;
            return false;
        }
        choosen = Tokens[_i++].Type;
        return true;
    }

    bool Match(TokenType expected)
    {
        if (IsAtEnd || expected != Tokens[_i].Type) {
            return false;
        }
        _i++;
        return true;
    }

    bool IsAtEnd => _i >= Tokens.Count || Tokens[_i].Type == Eof;

    int _iLastError = -1;
    void Error(string message)
    {
        if (_iLastError != _i) {
            _iLastError = _i;
            _reportError(_i, message);
        }
    }
}
