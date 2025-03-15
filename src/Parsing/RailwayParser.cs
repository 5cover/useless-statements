using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// An railway-oriented parser that produces error messages and returns null on error.
/// </summary>
/// 

/*
New ParseOperation implementation details
- Fewer lambdas
- Same method for terminals and non-terminals
- Should read like a recipe
- Direct translation of the railroad diagram
*/

/*
Problems : this parser doesnt have errors.
Errors have to be manually kept in sync with the parisng logic.

this is duplication
*/

sealed class RailwayParser : Parser
{
    internal delegate void ErrorReporter(int tokenIndex, string message);
    readonly ErrorReporter _reportError;

    /// <param name="tokens">The tokens to parse</param>
    /// <param name="reportError">The function to call to report an error (represented as a string)</param>
    public RailwayParser(ErrorReporter reportError)
    {
        _reportError = reportError;
    }

    public Node.Prog Parse() => Prog();

    #region Productions

    protected override Node.Prog Prog()
    {
        ZeroOrMore(Stmt, out var body);
        return new(body);
    }

    Node.Stmt? Stmt() => One(Semi) ? new Node.Stmt.Nop() : Expr();

    Node.Stmt.Expr? Expr()
     => First([Number, Grouping], out var expr)
     ? expr
     : null;

    Node.Stmt.Expr? Grouping()
     => One(LParen)
     && One(Expr, out var expr)
     && One(RParen)
     ? expr
     : null;

    Node.Stmt.Expr.Number? Number()
     => One(LitNumber, out decimal value)
     ? new(value)
     : null;

    #endregion Productions

    #region Fundamentals

    bool ZeroOrMore<TNode>(Func<TNode?> parser, out List<TNode> result) where TNode : class
    {
        result = [];
        while (!IsAtEnd) {
            int iStart = _i;
            var s = parser();
            if (s is null) {
                // todo: cause error here from the failed result
                if (iStart == _i) _i++;
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

    static bool One<TNode>(Func<TNode?> parser, [NotNullWhen(true)] out TNode? result) where TNode : class
     => (result = parser()) is not null;

    bool One<TValue>(TokenType expected, [NotNullWhen(true)] out TValue? value)
    {
        if (IsAtEnd || expected != Tokens[_i].Type) {
            value = default;
            return false;
        }
        var v = Tokens[_i++].Value;
        Debug.Assert(v is TValue);
        value = (TValue)v;
        return true;
    }

    bool One(TokenType[] expected, out TokenType choosen)
    {
        if (IsAtEnd || !expected.Contains(Tokens[_i].Type)) {
            choosen = default;
            return false;
        }
        choosen = Tokens[_i++].Type;
        return true;
    }

    bool One(TokenType expected)
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
    #endregion Fundamentals
}
