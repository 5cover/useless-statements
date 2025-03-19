using Scover.UselessStatements.Lexing;

using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// A helpful parser that produces error messages and returns null on error.
/// </summary>
/// <param name="reportError">The function to call to report an error (represented as a string)</param>
/// <remarks>
/// The helpful parser works by reporting errors when it fails to parse a terminal.
/// When a non-terminal fails (a parsing function returns <see langword="null"/>), it doesn't report an error, as the error has already been reported for the particular terminal that failed.
/// </remarks>
public sealed class HelpfulParser(Action<ParserError> reportError) : Parser(reportError)
{
    protected override Node.Prog Prog()
    {
        List<Node.Stmt> body = [];
        while (!IsAtEnd) {
            int iStart = I;
            var s = Stmt();
            if (s is not null) body.Add(s);
            // A Very Primitive Sychronization
            // but it prevents infinite loops if nothing was parsed.
            // Check if we read something before incrementing - otherwise we risk skipping valid tokens.
            // Example : `(5+;`. This prog fails in expr on `;`, but `;` should still be parsed as a valid nop.
            else if (iStart == I) I++;
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
        }
        if (Match(LParen)) {
            var expr = Expr();
            // Only one error per production rule. If Expr failed, it's probably irrelevant to try to make sense of what's next.
            // Example: `(5+;)`
            // 1. Error on first `;` for expression
            // 2. Error on first `;` for braced group
            // 3. Error on `)` for expression
            // This rule gets rid of error 2.
            // With something like `(5+)`, the `)` is still consumend and doesn't cause an expression error later.
            if (!Match(RParen) && expr is not null) Error("braced group", [RParen]);
            return expr;
        }

        Error("expression", [LitNumber, LParen]);

        return null;
    }

    Node.Stmt.Expr? ParseExprBinaryLeftAssociative(Func<Node.Stmt.Expr?> operand, TokenType[] operators)
    {
        Node.Stmt.Expr? expr = operand();
        if (expr is null) return null;
        while (Match(operators, out var op)) {
            var rhs = operand();
            if (rhs is null) return null;
            expr = new Node.Stmt.Expr.Binary(expr, op, rhs);
        }
        return expr;
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

    bool Match(IEnumerable<TokenType> expected, out TokenType choosen)
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
}
