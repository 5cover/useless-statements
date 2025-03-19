using Scover.UselessStatements;
using Scover.UselessStatements.Lexing;
using Scover.UselessStatements.Parsing;

namespace Scover.UselessStatementsTests;

public delegate Task<Node.Prog> ErrorHandlingParser(string input, IReadOnlyList<ParserError> errors);

public class ErrorHandlingParsersDataSourceAttribute : DataSourceGeneratorAttribute<ErrorHandlingParser>
{
    public static async Task<Node.Prog> DataOriented(string input, IReadOnlyList<ParserError> errors)
    {
        int nErrors = 0;
        var prog = Parse(input, new DataOrientedParser(async e => await Assert.That(e).IsEqualTo(errors[nErrors++])));
        await Assert.That(errors).HasCount().EqualTo(nErrors);
        return prog;
    }

    public static async Task<Node.Prog> Helpful(string input, IReadOnlyList<ParserError> errors)
    {
        int nErrors = 0;
        var prog = Parse(input, new HelpfulParser(async e => await Assert.That(e).IsEqualTo(errors[nErrors++])));
        await Assert.That(errors).HasCount().EqualTo(nErrors);
        return prog;
    }

    public override IEnumerable<Func<ErrorHandlingParser>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => Helpful;
        yield return () => DataOriented;
    }

    static Node.Prog Parse(string input, Parser p) => p.Parse([.. new Lexer(input).Lex()]);
}
