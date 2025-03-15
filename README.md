# Useless statements

A collection of useless parsers for an useless language.

## What is an error?

The idea is that if we want to build robust parsers we should precisely formalize errors instead of treating them as an afterthought.

The goal of a parser error could be to:

- Indicate what tokens to add to the program to fix the program
- Indicate what tokens to remove from the program to fix the program

What should be our goal?

- Disregarding as little of the user's code as possible, therefore recommending token removal only at a last resort?
- Fixing the error in as few steps as possible?
- **Fixing the error in as few keystrokes as possible?**

Each token type has an associated amount of keystrokes. For keywords it's simply the length of the keyword. For other token types, it's the length of the shortest valid form. (such as 1 for number).

When two fixes have the same amount of keystrokes, prefer the one that removes the least amount of tokens.

<style>del{color:red;}</style>
<style>ins{color:green;}</style>

### Error 1 : missing `)`

<pre>
(1 + 2<ins>)</ins>;
SyntaxError: insert `)` at offset 6 (after `2`) to complete braced group
</pre>

### Error 2 : missing expression right operand

<pre>
5 + <ins>&lt;number&gt;</ins>;
SyntaxError: insert a number at offset 4 (after `+`) to complete addition expression
</pre>

Other solution (not used since it removes 1 token while the above removes 0 tokens)

<pre>
5 <del>+</del> ;
SyntaxError: delete `+` at offset 2 (after `5`)
</pre>

### Error 4 : lone `+`

<pre>
<del>+</del>
SyntaxError: delete `+` at offset 0
</pre>

Other solution (not used since it incurs more keystrokes (2 against 1 for the above))

<pre>
<ins>&lt;number&gt;</ins>+<ins>&lt;number&gt;</ins>
SyntaxError: insert a number at offset 0 (before `+`) and at offset 1 (after `+`) to complete addition expression
</pre>

### Error 3 : complex error

<pre>
(5+<del>;</del><ins>&lt;number&gt;</ins>)
SyntaxError: replace `;` offset 3 (after `+`) by a number to complete addition expression
</pre>
