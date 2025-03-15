using Scover.UselessStatements.Lexing;
using Scover.UselessStatements.Parsing;

namespace Scover.UselessStatements;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0) {
            Console.Error.WriteLine($"usage: {Environment.ProcessPath} {{primitive|helpful|railway|data}} [INPUT]");
            Environment.ExitCode = 1;
            return;
        }

        var input = args.Length > 1 ? args[1] : "(5+;)";
        var tokens = new Lexer(input).Lex().ToArray();

        Parser parser;
        if ("primitive".StartsWith(args[0])) {
            parser = new PrimitiveParser();
        } else if ("helpful".StartsWith(args[0])) {
            parser = new HelpfulParser(ParseError);
        } else if ("railway".StartsWith(args[0])) {
            parser = new RailwayParser(ParseError);
        } else if ("data".StartsWith(args[0])) {
            parser = new DataOrientedParser(ParseError);
        } else {
            Console.Error.WriteLine($"{Environment.ProcessPath}: unknown parser: {args[0]}");
            Environment.ExitCode = 1;
            return;
        }

        var ast = parser.Parse(tokens);
        AstPrinter.PrettyPrint(ast);

        void ParseError(int iToken, string message) =>
            Console.Error.WriteLine($"Error on `{input[(Range)tokens[iToken].Extent]}` at offset {tokens[iToken].Extent.Start} : {message}");
    }

    public static void Error(string message) =>
        Console.Error.WriteLine($"Error: {message}");

}
