using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements;

static class Extensions
{
    static readonly ImmutableHashSet<TokenType> allTokenTypes = Enum.GetValues<TokenType>().ToImmutableHashSet();

    public static ImmutableHashSet<TokenType> Complement(this IReadOnlySet<TokenType> tokenTypes) => allTokenTypes.Except(tokenTypes);

    public static T NotNull<T>([NotNull] this T? t)
    {
        Debug.Assert(t is not null);
        return t;
    }

    public static void WriteLn(this TextWriter tw, int indent, object s)
    {
        for (int i = 0; i < indent * 2; ++i) {
            tw.Write(' ');
        }
        tw.WriteLine(s);
    }
}
