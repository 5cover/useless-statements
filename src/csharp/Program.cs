using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatements;

static class Program
{
    static void Main(string[] args)
    {
        var input = args.Length > 0 ? args[0] : "";
        var tokens = new Lexer(input).Lex().ToArray();
        var ast = new PrimitiveParser(tokens).Parse();
        AstPrinter.PrettyPrint(ast);
    }

    public static void Error(string message) => Console.Error.WriteLine($"Error: {message}");
}
