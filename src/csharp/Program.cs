using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements;

static class Program
{
    static void Main(string[] args)
    {
        var input = args.Length > 0 ? args[0] : "(5+)";
        var tokens = new Lexer(input).Lex().ToArray();
        var ast = new HelpfulParser(tokens, ParseError).Parse();
        AstPrinter.PrettyPrint(ast);

        void ParseError(int iToken, string message) =>
            Console.Error.WriteLine($"Error after `{input[tokens[iToken].Extent]}` at offset {tokens[iToken].Extent.Start} : {message}");
    }

    public static void Error(string message) =>
        Console.Error.WriteLine($"Error: {message}");

}
