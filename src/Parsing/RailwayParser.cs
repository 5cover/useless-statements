using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Scover.UselessStatements.Lexing;

using static Scover.UselessStatements.Lexing.TokenType;
using static Scover.UselessStatements.Parsing.RailwayParser;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// A railway-oriented parser that produces error messages and returns null on error.
/// </summary>
/// <param name="reportError">The function to call to report an error (represented as a string)</param>
/// 
/// New ParseOperation implementation details
/// - Fewer lambdas
/// - Same method for terminals and non-terminals
/// - Should read like a recipe
/// - Direct translation of the railroad diagram
///
/// Problems : this parser doesn't have errors.
/// Errors have to be manually kept in sync with the parsing logic.
/// this is duplication
sealed class RailwayParser(Action<ParserError> reportError) : Parser(reportError)
{
    protected override Node.Prog Prog()
    {
        ZeroOrMore(Stmt, out var body);
        return new(body);
    }

    Node.Stmt? Stmt() => One(Semi) ? new Node.Stmt.Nop() : Expr();

    Node.Stmt.Expr? Expr() => First([Number, Grouping], out var expr)
        ? expr
        : null;

    Node.Stmt.Expr? Grouping() 
        => One(LParen)
         && One(Expr, out var expr)
         && One(RParen)
                ? expr
                : null;

    Node.Stmt.Expr.Number? Number() => One(LitNumber, out decimal value)
        ? new(value)
        : null;

    #region Fundamentals

    bool ZeroOrMore<TNode>(Func<TNode?> parser, out List<TNode> result) where TNode : class
    {
        result = [];
        while (!IsAtEnd) {
            int iStart = I;
            var s = parser();
            if (s is null) {
                // todo: cause error here from the failed result
                if (iStart == I) I++;
            } else {
                result.Add(s);
            }
        }
        return true;
    }

    static bool First<TNode>(IEnumerable<Func<TNode?>> parsers, [NotNullWhen(true)] out TNode? result) where TNode : class
    {
        foreach (var p in parsers) {
            if ((result = p()) is not null) {
                return true;
            }
        }
        result = null;
        return false;
    }

    static bool One<TNode>(Func<TNode?> parser, [NotNullWhen(true)] out TNode? result) where TNode : class => (result = parser()) is not null;

    bool One<TValue>(TokenType expected, [NotNullWhen(true)] out TValue? value)
    {
        if (IsAtEnd || expected != Tokens[I].Type) {
            value = default;
            return false;
        }
        var v = Tokens[I++].Value;
        Debug.Assert(v is TValue);
        value = (TValue)v;
        return true;
    }

    bool One(IEnumerable<TokenType> expected, out TokenType choosen)
    {
        if (IsAtEnd || !expected.Contains(Tokens[I].Type)) {
            choosen = default;
            return false;
        }
        choosen = Tokens[I++].Type;
        return true;
    }

    bool One(TokenType expected)
    {
        if (IsAtEnd || expected != Tokens[I].Type) {
            return false;
        }
        I++;
        return true;
    }

    #endregion Fundamentals
}
