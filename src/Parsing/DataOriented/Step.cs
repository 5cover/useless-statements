using System.Collections.Immutable;
using System.Diagnostics;

using Scover.Option;
using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements.Parsing.DataOriented;

interface Step<out T>
{
    /// <summary>The allowed types of the first token.</summary>
    /// <remarks>There could be an issue if the head is required to be multiple tokens long.</remarks>
    ImmutableHashSet<TokenType> Head { get; }

    Result<T> Parse(ReadOnlySpan<Token> tokens, string parentName);
}

static class Altern
{
    public static Altern<T> Of<T>(string? name, IReadOnlyCollection<Step<T>> choices) => new(name, choices);
}

static class Concat
{
    public static Concat<T, T1, T2> Of<T, T1, T2>(
        string? name,
        Func<T1, Option<T2>, Option<T>> build,
        Step<T1> step1,
        Step<T2> step2
    ) => new(name, build, step1, step2);

    public static Concat<T, T1, T2, T3> Of<T, T1, T2, T3>(
        string? name,
        Func<T1, Option<T2>, Option<T3>, Option<T>> build,
        Step<T1> step1,
        Step<T2>? step2,
        Step<T3> step3
    ) => new(name, build, step1, step2, step3);
}

static class Repeat
{
    public static Repeat<T, TItem> Of<T, TItem>(string? name, Func<IReadOnlyList<TItem>, T> build, Step<TItem> step, ImmutableHashSet<TokenType>? tail = null, int min = 0, int max = int.MaxValue)
        => new(name, build, step, tail, min, max);
}

static class Terminal
{
    public static Terminal<T> Of<T>(string? name, Func<object?, T> build, TokenType type) => new(name, build, type);

    public static Terminal<TokenType> Of(string? name, TokenType type) => new(name, _ => type, type);
}

sealed class Altern<T> : Step<T>
{
    readonly IReadOnlyCollection<Step<T>> _choices;
    readonly string? _name;

    public Altern(string? name, IReadOnlyCollection<Step<T>> choices)
    {
        Debug.Assert(choices.Count > 0);
        Debug.Assert(choices.Sum(c => c.Head.Count) == choices.SelectMany(c => c.Head).Distinct().Count(),
            "all choices have distinct heads so no token can correspond to multiple choices");

        Head = [.. choices.SelectMany(c => c.Head)];
        _choices = choices;
        _name = name;
    }

    public ImmutableHashSet<TokenType> Head { get; }

    public Result<T> Parse(ReadOnlySpan<Token> tokens, string parentName)
    {
        foreach (var choice in _choices) {
            if (choice.Head.Contains(tokens[0].Type)) return choice.Parse(tokens, _name ?? parentName);
        }
        return Result.Fail<T>(0, [new(0, _name ?? parentName, Head)]);
    }
}

sealed class Concat<T, T1, T2>(
    string? name,
    Func<T1, Option<T2>, Option<T>> build,
    Step<T1> step1,
    Step<T2> step2
) : Step<T>

{
    public ImmutableHashSet<TokenType> Head => step1.Head;

    public Result<T> Parse(ReadOnlySpan<Token> tokens, string parentName)
    {
        int i = 0;
        List<ParserError> errors = [];

        var r1 = step1.Parse(tokens[i..], name ?? parentName);
        errors.AddRange(r1.BubbleErrors(i));
        i += r1.Read;
        if (!r1.HasValue) return Result.Fail<T>(i, errors);

        var r2 = step2.Parse(tokens[i..], name ?? parentName);
        errors.AddRange(r2.BubbleErrors(i));
        i += r2.Read;

        return build(r1.Value, r2).Match(
            value => Result.Ok(i, value, errors),
            () => Result.Fail<T>(i, errors));
    }
}

readonly struct NoValue
{
    public static NoValue Build(object? arg) => default;
}

// Zero for no min or max
sealed class Repeat<T, TItem> : Step<T>
{
    readonly Func<IReadOnlyList<TItem>, T> _build;
    readonly int _max;
    readonly int _min;
    readonly Step<TItem> _step;
    readonly ImmutableHashSet<TokenType> _tail;
    readonly string? _name;

    public Repeat(string? name, Func<IReadOnlyList<TItem>, T> build, Step<TItem> step, ImmutableHashSet<TokenType>? tail = null, int min = 0, int max = int.MaxValue)
    {
        Debug.Assert(min >= 0 && max >= 0 && max >= min);
        _build = build;
        _step = step;
        _tail = tail ?? _step.Head.Complement(); // optimization: instead of computing complement, keep null and perform lazy inverted check in Parse
        _name = name;
        _min = min;
        _max = max;
    }

    public ImmutableHashSet<TokenType> Head => _step.Head;

    public Result<T> Parse(ReadOnlySpan<Token> tokens, string parentName)
    {
        List<TItem> items = [];
        int i = 0;
        List<ParserError> errors = [];
        Result<TItem> r;

        // read what we must. fail if we don't have enough
        while (items.Count < _min) {
            r = _step.Parse(tokens[i..], _name ?? parentName);
            errors.AddRange(r.BubbleErrors(i));
            // fail if not matched
            i += r.Read;
            if (!r.HasValue) return Result.Fail<T>(i, errors);
            items.Add(r.Value);
        }

        // we've read what we must - we're in the safe zone. Read until max
        while (items.Count < _max && !_tail.Contains(tokens[i].Type)) {
            r = _step.Parse(tokens[i..], _name ?? parentName);
            // succeed early if not matched
            errors.AddRange(r.BubbleErrors(i));
            i += r.Read;
            if (r.HasValue) {
                items.Add(r.Value);
            } else if (r.Read == 0) {
                return Result.Ok(i, _build(items), errors);
            }
        }

        // we've read as many as we can. If there is more, it's a failure
        if (_tail.Contains(tokens[i].Type)) return Result.Ok(i, _build(items), errors);
        errors.Add(new(i, _name ?? parentName, _tail));
        return Result.Fail<T>(i, errors);
    }
}

sealed class Terminal<T>(string? name, Func<object?, T> build, TokenType type) : Step<T>
{
    public ImmutableHashSet<TokenType> Head { get; } = [type];

    public Result<T> Parse(ReadOnlySpan<Token> tokens, string parentName)
    {
        if (tokens.Length > 0 && tokens[0].Type == type) {
            return Result.Ok(1, build(tokens[0].Value));
        }
        return Result.Fail<T>(0, [new(0, name ?? parentName, Head)]);
    }
}

sealed class Concat<T, T1, T2, T3>(
    string? name,
    Func<T1, Option<T2>, Option<T3>, Option<T>> build,
    Step<T1> step1,
    Step<T2>? step2,
    Step<T3> step3
) : Step<T>

{
    Step<T2>? _step2 = step2;

    public ImmutableHashSet<TokenType> Head => step1.Head;

    public Result<T> Parse(ReadOnlySpan<Token> tokens, string parentName)
    {
        int i = 0;
        List<ParserError> errors = [];

        var r1 = step1.Parse(tokens[i..], name ?? parentName);
        errors.AddRange(r1.BubbleErrors(i));
        i += r1.Read;
        if (!r1.HasValue) return Result.Fail<T>(i, errors);

        var r2 = _step2.NotNull().Parse(tokens[i..], name ?? parentName);
        errors.AddRange(r2.BubbleErrors(i));
        i += r2.Read;

        var r3 = step3.Parse(tokens[i..], name ?? parentName);
        errors.AddRange(r3.BubbleErrors(i));
        i += r3.Read;

        return build(r1.Value, r2, r3).Match(
            value => Result.Ok(i, value, errors),
            () => Result.Fail<T>(i, errors));
    }

    public void SetStep2(Step<T2> value) => _step2 = value;
}
