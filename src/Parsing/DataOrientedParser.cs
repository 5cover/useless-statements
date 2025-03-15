using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata.Ecma335;
using Scover.UselessStatements.Lexing;

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

        // item     : 1..1
        // 0.item.* : 0..*
        // item.*   : 1..*

        // prog = 0.stmt.*, EOF
        // stmt = Semi | Expr

        // expr = expr_add

        // expr_add = expr_mult, 0.expr_add_right.*
        // expr_add_right = Plus | Minus, expr_mult

        // expr_mult = expr_primary, 0.expr_mult_right.*
        // expr_mult_right = Mul | Div | Mod, expr_primary

        // expr_primary = LitNumber | expr_group
        // expr_group = LParen, expr, RParen

        // So we have the operations
        //        , : Concat
        //        | : Altern
        // N.rule.N : Repeat
        Step? expr_add = null;

        Step expr_group = new Concat(() => [new Terminal(TokenType.LParen), expr_add ?? throw new UnreachableException(), new Terminal(TokenType.RParen)]);
        Step expr_primary = new Altern([new Terminal(TokenType.LitNumber), expr_group]);
        Step expr_mult_right = new Concat([
            new Altern([new Terminal(TokenType.Mul), new Terminal(TokenType.Div), new Terminal(TokenType.Mod)]),
            expr_primary
        ]);
        Step expr_mult = new Concat([expr_primary, new Repeat(expr_mult_right)]);
        Step expr_add_right = new Concat([
            new Altern([new Terminal(TokenType.Plus), new Terminal(TokenType.Minus)]),
            expr_mult,
        ]);
        expr_add = new Concat([expr_mult, new Repeat(expr_add_right)]);
        Step semi = new Terminal(TokenType.Semi);
        Step stmt = new Altern([semi, expr_add]);
        Step prog = new Repeat(stmt);

    }

    public Node.Prog Parse() => Prog();

    #region Productions

    protected override Node.Prog Prog()
    {
        return null!;
    }

    #endregion Productions

    interface Step<out T>
    {
        /// <summary>The allowed types of the first token.</summary>
        /// <remarks>There could be an issue if the head is required to be multiple tokens long.</remarks>
        IReadOnlySet<TokenType> Head { get; }
        Result<T> Parse(ReadOnlySpan<Token> tokens, int start);
    }

    sealed record Concat<T>(Func<IReadOnlyList<object>, T> Build) : Step
    {
        readonly Func<IReadOnlyList<Step<object>>>? _fsteps;
        IReadOnlyList<Step<object>>? _steps;
        public Concat(Func<IReadOnlyList<Step<object>>> steps) => _fsteps = steps;
        public Concat(IReadOnlyList<Step<object>> steps) => _steps = steps;
        public IReadOnlyList<Step<object>> Steps => _steps ??= (_fsteps ?? throw new UnreachableException())();
        public IReadOnlySet<TokenType> Head => Steps[0].Head;

        public Result<T> Parse(ReadOnlySpan<Token> tokens, int start)
        {
            List<object> subs = [];
            int end = start;
            foreach (var step in Steps) {
                Result<object> r = step.Parse(tokens, end);
                end = r.TokensExtent.End;
                if (r.HasValue) subs.Add(r.Value);
                else return Fail<T>(new(start, end), r.ExpectedNextTokenTypes);

            }
            return Ok(new(start, end), Build(subs));
        }

    }

    sealed record Altern<T> : Step<T>
    {
        public Altern(IReadOnlyCollection<Step<T>> choices)
        {
            Debug.Assert(choices.Count > 0);
            Debug.Assert(choices.Sum(c => c.Head.Count) == choices.SelectMany(c => c.Head).Distinct().Count(), "all choices have distinct heads so no token can correspond ot multiple choices");

            Head = choices.SelectMany(c => c.Head).ToHashSet();
            Choices = choices;

        }
        public IReadOnlyCollection<Step<T>> Choices { get; }
        public IReadOnlySet<TokenType> Head { get; }

        public Result<T> Parse(ReadOnlySpan<Token> tokens, int start)
        {
            if (start >= tokens.Length) return Fail<T>(default, Head);
            foreach (var choice in Choices) {
                if (choice.Head.Contains(tokens[0].Type)) return choice.Parse(tokens, start);
            }
            return Fail<T>(default, Head);
        }
    }

    // Zero for no min or max
    sealed record Repeat<T, TSub>(Func<IReadOnlyList<TSub>, T> Build) : Step<T>
    {
        public Repeat(Step<TSub> step, int min = 0, int max = 0)
        {
            Debug.Assert(min >= 0 && max >= 0 && max >= min);
            Step = step;
            Min = min;
            Max = max;
        }
        public Step<TSub> Step { get; }
        public int Min { get; }
        public int Max { get; }
        public IReadOnlySet<TokenType> Head => Step.Head;

        public Result<T> Parse(ReadOnlySpan<Token> tokens, int start)
        {
            if (start >= tokens.Length) return Fail<T>(default, Head);
            List<TSub> subs = [];
            int end = start;
            Result<TSub> r;

            // read what we must. fail if we don't have enough
            while (subs.Count < Min) {
                r = Step.Parse(tokens, end);
                // fail if not matched
                if (!r.HasValue) {
                    return Fail<T>(new(start, end), r.ExpectedNextTokenTypes);
                }
                end = r.TokensExtent.End;
                subs.Add(r.Value);
            }

            // we've read what we must - we're in the safe zone. Read until max
            while (subs.Count < Max) {
                r = Step.Parse(tokens, end);
                // succeed early if not matched
                if (!r.HasValue) return Ok(new(start, end), Build(subs));
                end = r.TokensExtent.End;
                subs.Add(r.Value);
            }

            // we've read as many as we can. If there is one more, it's a failure
            r = Step.Parse(tokens, end);
            return r.HasValue
                ? FailComplement<T>(new(start, end), r.ExpectedNextTokenTypes)
                : Ok(new(start, end), Build(subs));
        }
    }

    sealed record Terminal<T>(Func<object?, T> Build, TokenType Type) : Step<T>
    {
        public IReadOnlySet<TokenType> Head { get; } = new HashSet<TokenType>(1) { Type };

        public int Match(ReadOnlySpan<Token> tokens) => tokens.Length > 0 && tokens[0].Type == Type ? 1 : 0;
        public Result<T> Parse(ReadOnlySpan<Token> tokens, int start)
         => tokens.Length > 0 && tokens[0].Type == Type
            ? Ok(FixedRange.Of(start, 1), Build(tokens[0].Value))
            : Fail<T>(default, Head);
    }

    interface Result<out T>
    {
        bool HasValue { get; }
        FixedRange TokensExtent { get; }

        [MemberNotNullWhen(true, nameof(HasValue))]
        T? Value { get; }

        [MemberNotNullWhen(false, nameof(HasValue))]
        IReadOnlySet<TokenType>? ExpectedNextTokenTypes { get; }
    }

    readonly record struct ResultImpl<T> : Result<T>
    {
        public bool HasValue => Value is not null;
        public FixedRange TokensExtent { get; init; }

        [MemberNotNullWhen(true, nameof(HasValue))]
        public T? Value { get; init; }

        [MemberNotNullWhen(false, nameof(HasValue))]
        public IReadOnlySet<TokenType>? ExpectedNextTokenTypes { get; init; }
    }

    static ResultImpl<TNode> Ok<TNode>(FixedRange tokens_extent, TNode value)
     => new ResultImpl<TNode>() { TokensExtent = tokens_extent, Value = value };

    static ResultImpl<TNode> Fail<TNode>(FixedRange tokens_extent, IReadOnlySet<TokenType> next_expected)
     => new ResultImpl<TNode>() { TokensExtent = tokens_extent, ExpectedNextTokenTypes = next_expected };
}
