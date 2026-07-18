using System.Text;

namespace eQuantic.Linq.Web.Syntax;

/// <summary>Character scanner shared by the query-string syntax parsers.</summary>
internal sealed class SyntaxReader
{
    private readonly string _text;
    private int _position;

    public SyntaxReader(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public int Position => _position;

    public bool End
    {
        get
        {
            SkipWhitespace();
            return _position >= _text.Length;
        }
    }

    public void Seek(int position) => _position = position;

    public char Peek() => _text[_position];

    public void SkipWhitespace()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
        {
            _position++;
        }
    }

    public bool TryConsume(char expected)
    {
        SkipWhitespace();
        if (_position < _text.Length && _text[_position] == expected)
        {
            _position++;
            return true;
        }

        return false;
    }

    public void Expect(char expected)
    {
        if (!TryConsume(expected))
        {
            throw Error($"'{expected}' expected");
        }
    }

    /// <summary>Reads an identifier: letters, digits and underscores, starting with a letter or underscore.</summary>
    public string ReadIdentifier()
    {
        SkipWhitespace();
        var start = _position;

        while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_'))
        {
            _position++;
        }

        if (_position == start)
        {
            throw Error("identifier expected");
        }

        return _text.Substring(start, _position - start);
    }

    /// <summary>
    /// Reads a literal value: either a single-quoted string (with <c>''</c> escaping) or raw text up to
    /// the next top-level <c>,</c> / <c>)</c> (inner parentheses are balanced). The unquoted keyword
    /// <c>null</c> yields <see langword="null"/>.
    /// </summary>
    public string? ReadValue() => ReadValueCore(stopAtPipe: false);

    /// <summary>Same as <see cref="ReadValue"/> but also stops at a top-level <c>|</c> (for <c>in</c>/<c>nin</c> lists).</summary>
    public string? ReadValueUntilPipeOrParenthesis() => ReadValueCore(stopAtPipe: true);

    private string? ReadValueCore(bool stopAtPipe)
    {
        SkipWhitespace();

        if (_position < _text.Length && _text[_position] == '\'')
        {
            _position++;
            var builder = new StringBuilder();

            while (true)
            {
                if (_position >= _text.Length)
                {
                    throw Error("unterminated quoted value");
                }

                var current = _text[_position++];
                if (current == '\'')
                {
                    if (_position < _text.Length && _text[_position] == '\'')
                    {
                        builder.Append('\'');
                        _position++;
                        continue;
                    }

                    break;
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        var start = _position;
        var depth = 0;

        while (_position < _text.Length)
        {
            var current = _text[_position];

            if (current == '(')
            {
                depth++;
            }
            else if (current == ')')
            {
                if (depth == 0)
                {
                    break;
                }

                depth--;
            }
            else if (current == ',' && depth == 0)
            {
                break;
            }
            else if (stopAtPipe && current == '|' && depth == 0)
            {
                break;
            }

            _position++;
        }

        var raw = _text.Substring(start, _position - start).Trim();
        return raw.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : raw;
    }

    public QueryStringParseException Error(string message) => new(message, _position, _text);
}
