parser grammar UselessStatementsParser;

options {
    tokenVocab = UselessStatementsLexer;
}

prog: stmt* EOF;

stmt: expr? Semi;

expr: expr_add;

expr_add: expr_mult ((Plus | Minus) expr_add)*;

expr_mult: expr_primary ((Mul | Div | Mod) expr_primary)*;

expr_primary: LitNumber;
