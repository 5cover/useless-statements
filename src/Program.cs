using Scover.UselessStatements.Lexing;
using Scover.UselessStatements.Parsing;

namespace Scover.UselessStatements;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2) {
            Console.Error.WriteLine($"usage: {Environment.ProcessPath} {{primitive|helpful|railway|data}} [INPUT]");
            Environment.ExitCode = 1;
            return;
        }

        var lexer = new Lexer(args[1]);
        var tokens = lexer.Lex().ToArray();

        while (lexer.TryGetError(out var e)) {
            SyntaxError(e.Verb, e.Subject, FixedRange.Of(e.Index, 1), args[1][e.Index].ToString(), "");
        }

        Parser parser;
        if ("primitive".StartsWith(args[0])) {
            parser = new PrimitiveParser();
        } else if ("helpful".StartsWith(args[0])) {
            parser = new HelpfulParser(ParseError);
        } /*else if ("railway".StartsWith(args[0])) {
            parser = new RailwayParser(ParseError);
        } else if ("data".StartsWith(args[0])) {
            parser = new DataOrientedParser(ParseError);
        }*/ else {
            Console.Error.WriteLine($"{Environment.ProcessPath}: unknown parser: {args[0]}");
            Environment.ExitCode = 1;
            return;
        }

        var ast = parser.Parse(tokens);
        AstPrinter.PrettyPrint(ast);

        void ParseError(SyntaxError e)
        {
            bool isFirst = e.Index == 0;
            bool isLast = e.Index >= tokens.Length - 2; // eof doesn't count
            string closest =
                !isFirst ? $" (after `{args[1][(Range)tokens[e.Index - 1].Extent]}`)"
                : !isLast ? $" (before `{args[1][(Range)tokens[e.Index + 1].Extent]}`)"
                : "";
            var r = tokens[e.Index].Extent;
            SyntaxError(e.Verb, e.Subject, r, args[1][(Range)r], closest);
        }
    }

    static void SyntaxError(ErrorVerb verb, string subject, FixedRange range, string current, string closest)
    {
        Console.Error.Write("SyntaxError: ");
        switch (verb) {
        case ErrorVerb.Insert:
            Console.Error.WriteLine($"insert {subject} at offset {range.End - 1}{closest}");
            break;
        case ErrorVerb.Replace:
            Console.Error.WriteLine(current == ""
                ? $"insert {subject} at offset {range.End - 1}{closest}"
                : $"replace `{current}` at offset {range.Start}{closest} by {subject}");
            break;
        }
    }

}
