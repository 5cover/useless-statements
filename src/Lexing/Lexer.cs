using static System.Globalization.CultureInfo;

using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Lexing;

public sealed class Lexer(string input)
{
    readonly Queue<LexerError> _errors = new();
    int _start;
    int _i;

    public bool TryGetError(out LexerError error) => _errors.TryDequeue(out error);

    public IEnumerable<Token> Lex()
    {
        while (!IsAtEnd) {
            _start = _i;
            char c = Advance();
            switch (c) {
            case ';': yield return Ok(Semi); break;
            case '+': yield return Ok(Plus); break;
            case '-': yield return Ok(Minus); break;
            case '/': yield return Ok(Div); break;
            case '*': yield return Ok(Mul); break;
            case '%': yield return Ok(Mod); break;
            case '(': yield return Ok(LParen); break;
            case ')': yield return Ok(RParen); break;
            case '.' when MatchDigit():
                while (MatchDigit()) { }
                yield return OkDecimal();
                break;
            default:
                if (char.IsAsciiDigit(c)) {
                    while (MatchDigit()) { }
                    if (Match('.')) {
                        if (MatchDigit()) {
                            while (MatchDigit()) { }
                            yield return OkDecimal();
                        } else {
                            Error("expected digit");
                        }
                    } else {
                        while (MatchDigit()) { }
                        yield return OkDecimal();
                    }
                } else if (!char.IsWhiteSpace(c)) {
                    Error($"stray `{c}`");
                }
                break;
            }
        }
        _start = _i;
        yield return Ok(Eof);
    }

    void Error(string message) => _errors.Enqueue(new(_i - 1, message));

    Token OkDecimal() => new(new(_start, _i), LitNumber, decimal.Parse(Lexeme, InvariantCulture));
    Token Ok(TokenType type) => new(new(_start, _i), type);

    ReadOnlySpan<char> Lexeme => input.AsSpan()[_start.._i];

    char Advance() => input[_i++];

    bool MatchDigit()
    {
        if (IsAtEnd || !char.IsAsciiDigit(input[_i])) return false;
        ++_i;
        return true;
    }

    bool Match(char expected)
    {
        if (IsAtEnd || input[_i] != expected) return false;
        ++_i;
        return true;
    }
    bool IsAtEnd => _i >= input.Length;
}
