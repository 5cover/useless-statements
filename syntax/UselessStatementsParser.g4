parser grammar UselessStatementsParser;

options {
    tokenVocab = UselessStatementsLexer;
}

prog: stmt* EOF;

stmt: Semi | expr;

expr: expr_add;

expr_add: expr_mult ((Plus | Minus) expr_add)*;

expr_mult: expr_primary ((Mul | Div | Mod) expr_primary)*;

expr_primary: LitNumber | LParen expr RParen;
