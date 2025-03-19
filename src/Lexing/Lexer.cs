using static System.Globalization.CultureInfo;

using static Scover.UselessStatements.Lexing.TokenType;

namespace Scover.UselessStatements.Lexing;

public sealed class Lexer(string input)
{
    readonly Queue<LexerError> _errors = new();
    int _i;
    int _start;
    bool IsAtEnd => _i >= input.Length;

    ReadOnlySpan<char> Lexeme => input.AsSpan()[_start.._i];

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

    public bool TryGetError(out LexerError error) => _errors.TryDequeue(out error);

    char Advance() => input[_i++];

    void Error(string message) => _errors.Enqueue(new(_i - 1, message));

    bool Match(char expected)
    {
        if (IsAtEnd || input[_i] != expected) return false;
        ++_i;
        return true;
    }

    bool MatchDigit()
    {
        if (IsAtEnd || !char.IsAsciiDigit(input[_i])) return false;
        ++_i;
        return true;
    }

    Token Ok(TokenType type) => new(new(_start, _i), type);

    Token OkDecimal() => new(new(_start, _i), LitNumber, decimal.Parse(Lexeme, InvariantCulture));
}
