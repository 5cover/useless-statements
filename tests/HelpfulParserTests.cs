﻿using Scover.UselessStatements.Lexing;

namespace Scover.UselessStatementsTests;

public class HelpfulParserTests
{
    static readonly ErrorHandlingParser parse = ErrorHandlingParsersDataSourceAttribute.Helpful;

    [Test]
    public async Task MissingOperator()
    {
        var prog = await parse("(5 4)", [
            new(2, "braced group", new HashSet<TokenType>() { TokenType.RParen }),
            new(3, "expression", new HashSet<TokenType>() { TokenType.LParen, TokenType.LitNumber }),
        ]);
        await Assert.That(prog.Body).HasCount().EqualTo(2);
        using var _ = Assert.Multiple();
        await Assert2.Number(prog.Body[0], 5);
        await Assert2.Number(prog.Body[1], 4);
    }
}
