using Scover.UselessStatements.Lexing;
using Scover.UselessStatements.Parsing;

namespace Scover.UselessStatements;

static class Program
{
    static void Main(string[] args)
    {
        if (args is not [var parserName, var input]) {
            Console.Error.WriteLine($"usage: {Environment.ProcessPath} {{primitive|helpful|railway|data}} [INPUT]");
            Environment.ExitCode = 1;
            return;
        }

        var lexer = new Lexer(input);
        var tokens = lexer.Lex().ToArray();

        while (lexer.TryGetError(out var e)) {
            SyntaxError(FixedRange.Of(e.Index, 1), e.Message);
        }

        Parser parser;
        if ("primitive".StartsWith(parserName)) {
            parser = new PrimitiveParser();
        } else if ("helpful".StartsWith(parserName)) {
            parser = new HelpfulParser(ParseError);
        } /*else if ("railway".StartsWith(parserName)) {
            parser = new RailwayParser(ParseError);
        } else if ("data".StartsWith(parserName)) {
            parser = new DataOrientedParser(ParseError);
        }*/ else {
            Console.Error.WriteLine($"{Environment.ProcessPath}: unknown parser: {parserName}");
            Environment.ExitCode = 1;
            return;
        }

        var ast = parser.Parse(tokens);
        ast.PrettyPrint();

        void ParseError(ParserError e)
        {
            bool isFirst = e.Index == 0;
            bool isLast = e.Index >= tokens.Length - 2; // eof doesn't count
            string closest =
                !isFirst ? $" (after `{input[(Range)tokens[e.Index - 1].Extent]}`)"
                : !isLast ? $" (before `{input[(Range)tokens[e.Index + 1].Extent]}`)"
                : "";
            SyntaxError(tokens[e.Index].Extent, $"expected {string.Join(" or ", e.Expected)} for {e.Subject}, got {tokens[e.Index].Type}", closest);
        }

        void SyntaxError(
            FixedRange range,
            string message,
            string closest = ""
        ) => Console.Error.WriteLine(
            $"syntax error at offset {range.Start}..{range.End}{closest}: {message}: {input[(Range)range]}"
        );
    }
}
