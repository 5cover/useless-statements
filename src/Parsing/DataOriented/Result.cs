using System.Diagnostics.CodeAnalysis;

using Scover.Options;

namespace Scover.UselessStatements.Parsing.DataOriented;

static class Result
{
    public static Result<T> Fail<T>(int read, IEnumerable<ParserError>? errors = null) =>
        new ResultImpl<T>(false, read, default, errors);

    public static Result<T> Ok<T>(int read, T value, IEnumerable<ParserError>? errors = null) =>
        new ResultImpl<T>(true, read, value, errors);

    sealed class ResultImpl<T>(bool hasValue, int read, T? value, IEnumerable<ParserError>? errors) : Result<T>
    {
        public int Read => read;
        public bool HasValue => hasValue;
        public T? Value => value;
        public IEnumerable<ParserError> Errors => errors ?? [];

        /// <inheritdoc />
        public IEnumerable<ParserError> BubbleErrors(int i) => errors is null ? [] : errors.Select(e => e with { Index = e.Index + i });
    }
}

interface Result<out T> : Option<T>
{
    int Read { get; }
    IEnumerable<ParserError> Errors { get; }
    IEnumerable<ParserError> BubbleErrors(int i);
}
