using Scover.UselessStatements;
using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatementsTests;

public class LexerTests
{
    static async Task AssertErrors(Lexer l, IReadOnlyList<LexerError> errors)
    {
        int i = 0;
        while (l.TryGetError(out var e)) {
            await Assert.That(e).IsEqualTo(errors[i++]);
        }
        await Assert.That(errors).HasCount().EqualTo(i);
    }
    static async Task AssertIsEofToken(string input, Token t) => await AssertToken(t, TokenType.Eof, input.Length, input.Length);

    static async Task AssertRange(FixedRange r, int start, int end)
    {
        using var _ = Assert.Multiple();
        await Assert.That(r.Start).IsEqualTo(start);
        await Assert.That(r.End).IsEqualTo(end);
        await Assert.That(r.Length).IsEqualTo(end - start);
    }

    static async Task AssertToken(Token t, TokenType type, int start, int end, object? value = null)
    {
        using var _ = Assert.Multiple();
        await Assert.That(t.Type).IsEqualTo(type);
        await AssertRange(t.Extent, start, end);
        await Assert.That(t.Value).IsEqualTo(value);
    }

    [Test]
    [Arguments("+", TokenType.Plus)]
    [Arguments("/", TokenType.Div)]
    [Arguments("%", TokenType.Mod)]
    [Arguments("%", TokenType.Mod)]
    [Arguments("*", TokenType.Mul)]
    [Arguments(";", TokenType.Semi)]
    [Arguments("(", TokenType.LParen)]
    [Arguments(")", TokenType.RParen)]
    public async Task SingleTokens(string input, TokenType tokenType)
    {
        var l = new Lexer(input);
        var tokens = l.Lex().ToArray();
        await AssertErrors(l, []);
        await Assert.That(tokens).HasCount().EqualTo(2);
        await AssertIsEofToken(input, tokens[1]);
        await AssertToken(tokens[0], tokenType, 0, 1);
    }

    [Test]
    [Arguments("123", 123)]
    [Arguments("123.45", 123.45)]
    [Arguments("00123.45", 123.45)]
    [Arguments("1000.117", 1000.117)]
    [Arguments("123.4500", 123.45)]
    [Arguments("0.0", 0)]
    [Arguments("0", 0)]
    [Arguments(".45", .45)]
    [Arguments("0.45", .45)]
    public async Task NumberLiterals(string input, decimal value)
    {
        var l = new Lexer(input);
        var tokens = l.Lex().ToArray();
        await AssertErrors(l, []);
        await Assert.That(tokens).HasCount().EqualTo(2);
        await AssertIsEofToken(input, tokens[1]);
        await AssertToken(tokens[0], TokenType.LitNumber, 0, input.Length, value);
    }

    [Test]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments("     ")]
    [Arguments(" \r\n\t\v    ")]
    public async Task EmptyInput(string input)
    {
        var l = new Lexer(input);
        var tokens = l.Lex().ToArray();
        await AssertErrors(l, []);
        await AssertIsEofToken(input, await Assert.That(tokens).HasSingleItem());
    }

    [Test]
    public async Task MixedInput()
    {
        const string Input = "123 + 45.67 * (89 / 2) % 3;";
        var l = new Lexer(Input);
        var tokens = l.Lex().ToArray();
        await AssertErrors(l, []);
        await Assert.That(tokens).HasCount().EqualTo(13);
        using var _ = Assert.Multiple();
        await AssertIsEofToken(Input, tokens[12]);
        await AssertToken(tokens[0], TokenType.LitNumber, 0, 3, 123M);
        await AssertToken(tokens[1], TokenType.Plus, 4, 5);
        await AssertToken(tokens[2], TokenType.LitNumber, 6, 11, 45.67M);
        await AssertToken(tokens[3], TokenType.Mul, 12, 13);
        await AssertToken(tokens[4], TokenType.LParen, 14, 15);
        await AssertToken(tokens[5], TokenType.LitNumber, 15, 17, 89M);
        await AssertToken(tokens[6], TokenType.Div, 18, 19);
        await AssertToken(tokens[7], TokenType.LitNumber, 20, 21, 2M);
        await AssertToken(tokens[8], TokenType.RParen, 21, 22);
        await AssertToken(tokens[9], TokenType.Mod, 23, 24);
        await AssertToken(tokens[10], TokenType.LitNumber, 25, 26, 3M);
        await AssertToken(tokens[11], TokenType.Semi, 26, 27);
    }

    [Test]
    public async Task InvalidNumber()
    {
        const string Input = "123..45";
        var l = new Lexer(Input);
        var tokens = l.Lex().ToArray();
        await AssertErrors(l, [new(3, "expected digit")]);
        await Assert.That(tokens).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await AssertIsEofToken(Input, tokens[1]);
        await AssertToken(tokens[0], TokenType.LitNumber, 4, 7, .45M);
    }

    [Test]
    [Arguments("&")]
    [Arguments("@")]
    public async Task InvalidSequence(string input)
    {
        var l = new Lexer(input);
        var tokens = l.Lex().ToArray();
        await AssertErrors(l, [new(0, $"stray `{input}`")]);
        await AssertIsEofToken(input, await Assert.That(tokens).HasSingleItem());
    }
}
