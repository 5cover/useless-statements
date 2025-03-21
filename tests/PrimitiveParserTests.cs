﻿using Scover.UselessStatements.Lexing;
using Scover.UselessStatements.Parsing;

namespace Scover.UselessStatementsTests;

public class PrimitiveParserTests
{
    static Node.Prog Parse(string input)
    {
        var l = new Lexer(input);
        var tokens = l.Lex().ToArray();
        var p = new PrimitiveParser();
        return p.Parse(tokens);
    }
    [Test]
    public async Task EmptyProgram()
    {
        var prog = Parse("");
        await Assert.That(prog.Body).IsEmpty();
    }

    [Test]
    public async Task SingleNop()
    {
        var prog = Parse(";");
        await Assert.That(await Assert.That(prog.Body).HasSingleItem()).IsTypeOf<Node.Stmt.Nop>();
    }

    [Test]
    public async Task SingleExprStmt()
    {
        var prog = Parse("123");
        await Assert2.Number(await Assert.That(prog.Body).HasSingleItem(), 123M);
    }

    [Test]
    public async Task MultipleStmts()
    {
        var prog = Parse("123 45.67");
        await Assert.That(prog.Body).HasCount().EqualTo(2);
        await Assert2.Number(prog.Body[0], 123M);
        await Assert2.Number(prog.Body[1], 45.67M);
    }

    [Test]
    public async Task ExprAdd()
    {
        var prog = Parse("123 + 45.67");
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Plus);
        await Assert2.Number(e1.Lhs, 123M);
        await Assert2.Number(e1.Rhs, 45.67M);
    }

    [Test]
    public async Task ExprSub()
    {
        var prog = Parse("123 - 45.67");
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Minus);
        await Assert2.Number(e1.Lhs, 123M);
        await Assert2.Number(e1.Rhs, 45.67M);
    }

    [Test]
    public async Task ExprMul()
    {
        var prog = Parse("123 * 45.67");
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Mul);
        await Assert2.Number(e1.Lhs, 123M);
        await Assert2.Number(e1.Rhs, 45.67M);
    }

    [Test]
    public async Task ExprMod()
    {
        var prog = Parse("123 % 45.67");
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Mod);
        await Assert2.Number(e1.Lhs, 123M);
        await Assert2.Number(e1.Rhs, 45.67M);
    }

    [Test]
    public async Task ExprDiv()
    {
        var prog = Parse("123 / 45.67");
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Div);
        await Assert2.Number(e1.Lhs, 123M);
        await Assert2.Number(e1.Rhs, 45.67M);
    }

    [Test]
    [Arguments("(123 / 45.67)")]
    [Arguments("(123 / (45.67))")]
    [Arguments("123 / (45.67)")]
    [Arguments("(123) / (45.67)")]
    [Arguments("((123) / (45.67))")]
    [Arguments("((((123) / 45.67)))")]
    public async Task ExprGrouped(string input)
    {
        var prog = Parse(input);
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Div);
        await Assert2.Number(e1.Lhs, 123M);
        await Assert2.Number(e1.Rhs, 45.67M);
    }

    [Test]
    [Arguments("1 + 2 * 3")]
    [Arguments("(1) + 2 * 3")]
    [Arguments("(1) + (2) * 3")]
    [Arguments("(1) + (2) * (3)")]
    [Arguments("1 + (2 * 3)")]
    [Arguments("(1) + (2 * 3)")]
    [Arguments("(1 + 2 * 3)")]
    [Arguments("(1) + 2 * (3)")]
    public async Task ExprNested(string input)
    {
        var prog = Parse(input);

        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Plus);
        await Assert2.Number(e1.Lhs, 1M);

        var e2 = await Assert2.Binary(e1.Rhs, TokenType.Mul);
        await Assert2.Number(e2.Lhs, 2M);
        await Assert2.Number(e2.Rhs, 3M);
    }

    [Test]
    [Arguments("(1 + 2) * 3")]
    [Arguments("(1 + 2) * (3)")]
    [Arguments("((1) + 2) * 3")]
    [Arguments("((1) + (2)) * 3")]
    [Arguments("((1) + (2)) * (3)")]
    [Arguments("(1 + (2)) * 3")]
    public async Task ExprNested2(string input)
    {
        var prog = Parse(input);

        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Mul);
        await Assert2.Number(e1.Rhs, 3M);

        var e2 = await Assert2.Binary(e1.Lhs, TokenType.Plus);
        await Assert2.Number(e2.Lhs, 1M);
        await Assert2.Number(e2.Rhs, 2M);
    }

    [Test]
    public async Task ExprComplex()
    {
        var prog = Parse("1 + 2 * (3 - 4) / 5 % 6");
        var e1 = await Assert2.Binary(await Assert.That(prog.Body).HasSingleItem(), TokenType.Plus);
        await Assert2.Number(e1.Lhs, 1M);

        var e2 = await Assert2.Binary(e1.Rhs, TokenType.Mod);
        await Assert2.Number(e2.Rhs, 6M);

        var e3 = await Assert2.Binary(e2.Lhs, TokenType.Div);
        await Assert2.Number(e3.Rhs, 5M);

        var e4 = await Assert2.Binary(e3.Lhs, TokenType.Mul);
        await Assert2.Number(e4.Lhs, 2M);

        var e5 = await Assert2.Binary(e4.Rhs, TokenType.Minus);
        await Assert2.Number(e5.Lhs, 3M);
        await Assert2.Number(e5.Rhs, 4M);
    }

    [Test]
    public async Task MissingOperand()
    {
        var prog = Parse("1 +");
        await Assert.That(prog.Body).IsEmpty();
    }

    [Test]
    public async Task MissingOperandFollowedByNop()
    {
        var prog = Parse("1 + ;");
        await Assert.That(await Assert.That(prog.Body).HasSingleItem()).IsTypeOf<Node.Stmt.Nop>();
    }

    [Test]
    public async Task MismatchedParentheses()
    {
        var prog = Parse("(1 + 2");
        await Assert.That(prog.Body).IsEmpty();
    }
}
