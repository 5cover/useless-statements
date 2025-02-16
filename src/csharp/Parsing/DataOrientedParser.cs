using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Scover.UselessStatements.Lexing;
using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Parsing;

/// <summary>
/// Data-oriented parser
/// </summary>

sealed class DataOrientedParser : Parser
{
    internal delegate void ErrorReporter(int tokenIndex, string message);
    readonly ErrorReporter _reportError;

    /// <param name="tokens">The tokens to parse</param>
    /// <param name="reportError">The function to call to report an error (represented as a string)</param>
    public DataOrientedParser(ErrorReporter reportError)
    {
        _reportError = reportError;
    }

    public Node.Prog Parse() => Prog();

    #region Productions

    /*readonly Production<Node.Stmt.Expr.Number> _number;
    ParseResult<Node.Stmt.Expr.Number> RunNumber(Token head, int start)
    {
        Debug.Assert(head.Value is decimal);
        return ParseResult.Ok<Node.Stmt.Expr.Number>(0, new((decimal)head.Value));
    }

    readonly Production<Node.Stmt.Expr.Number> _parenNumber;
    ParseResult<Node.Stmt.Expr.Number> RunParenNumber(Token head, int start)
    {
        var r = _number.Parse(Tokens[start], start + 1);
        if (Tokens[r.Read].Type is not RParen) {
            return ParseResult.Fail<Node.Stmt.Expr.Number>(r.Read, _parenNumber, Tokens[r.Read]);
        }
        return r with { Read = r.Read + 1 };
    }*/

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

    readonly struct Result<TNode>
    {
        public bool HasValue { get; init; }
        [MemberNotNullWhen(true, nameof(HasValue))]
        public TNode? Value { get; init; }
        [MemberNotNullWhen(false, nameof(HasValue))]
        public IReadOnlyCollection<Token>? Expected { get; init; }
    }

    static Result<TNode> Ok<TNode>(TNode value) => new() { HasValue = true, Value = value };
    static Result<TNode> Fail<TNode>(IReadOnlyCollection<Token> expected) => new() { Expected = expected };

    #endregion Fundamentals
}


/*
What is a parser?

A parser is a (non-empty, possibily infinite) sequence of expected terminals (tokens). We may reuse other parsers to build this sequence.

This sequence knows how to turn itself into a value, which may be an AST node (will probably be an AST node), or no value, just a successful result.

Seeing parsers with way instead of opaque functions ables us to work with them in smart and elegant ways, for performance and error handling.

No more control flow magic, that's my objective.
*/

delegate ParseResult<T> Runner<T>(Token head, int iStart);

readonly struct Production<T>(string name, IReadOnlyCollection<TokenType> head, Runner<T> run) : Production
{
    public string Name => name;
    public IReadOnlyCollection<TokenType> Head => head;
    public ParseResult<T> Parse(Token head, int iStart)
    {
        if (Head.Contains(head.Type)) {
            var r = run(head, iStart);
            return r with { Read = 1 + r.Read };
        } else {
            return ParseResult.Fail<T>(0, this, head);
        }
    }
}

interface Production
{
    string Name { get; }
    IReadOnlyCollection<TokenType> Head { get; }
}

static class ParseResult
{
    public static ParseResult<T> Ok<T>(int read, T value) => new() { Read = read, HasValue = true, Value = value };
    public static ParseResult<T> Fail<T>(int read, Production parser, Token failedAt) => new() { Read = read, Parser = parser, FailedAt = failedAt, };
}

readonly record struct ParseResult<T>
{
    /// <summary>
    /// Number of tokens the parser has read successfully (that it expected).
    /// </summary>
    public int Read { get; init; }
    public bool HasValue { get; init; }
    [MemberNotNullWhen(true, nameof(HasValue))]
    public T? Value { get; init; }
    [MemberNotNullWhen(false, nameof(HasValue))]
    public Production? Parser { get; init; }
    /// <summary>
    /// The token that made the parser fail. May be the Eof token to indicate that the parser failed at the end of the token sequence.
    /// </summary>
    [MemberNotNullWhen(false, nameof(HasValue))]
    public Token FailedAt { get; init; }
}
