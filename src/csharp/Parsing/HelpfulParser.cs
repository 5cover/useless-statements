using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements;

/// <summary>
/// An helpful parser that produces error messages and returns null on error.
/// </summary>
/// <param name="tokens">The tokens to parse</param>
/// <param name="reportError">The function to call to report an error (represented as a string)</param>
/// <remarks>
/// The helpful parser works by reporting errors when it fails to parse a terminal.
/// When a non-terminal fails (a parsing function returns <see langword="null"/>), it doesn't report an error, as the error has already been reported for the particular terminal that failed.
/// </remarks>
sealed class HelpfulParser(IReadOnlyList<Token> tokens, HelpfulParser.ErrorReporter reportError)
{
    internal delegate void ErrorReporter(int tokenIndex, string message);
    readonly IReadOnlyList<Token> _tokens = tokens;
    readonly ErrorReporter _reportError = reportError;
    int _i;

    public Node.Prog Parse() => Prog();

    Node.Prog Prog()
    {
        List<Node.Stmt> body = [];
        while (!IsAtEnd) {
            var s = Stmt();
            if (s is not null) body.Add(s);
            // A Very Primitive Sychronization
            // but it prevents infinite loops if nothing was parsed.
            // but could this hide errors if we something had been parsed? todo
            else _i++; // Might go outside _tokens but that's ok since IsAtEnd checks for that
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

    bool IsAtEnd => _i >= _tokens.Count || _tokens[_i].Type == Eof;

    void Error(string message) => _reportError(_i, message);
}
