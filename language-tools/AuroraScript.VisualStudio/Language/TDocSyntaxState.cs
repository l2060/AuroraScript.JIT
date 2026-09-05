using System.Collections.Generic;

namespace AuroraScript.VisualStudio.Language;

// Consumes the tagger's existing token stream; comments and whitespace never alter this state.
internal sealed class TDocSyntaxState
{
    private readonly Stack<char> _containers = new();
    private bool _expectsKey;
    private bool _expectsTypedKey;

    public TDocTokenRole Identifier(string value, bool followedByPropertyName)
    {
        if (_expectsKey)
        {
            if (!_expectsTypedKey && value == "readonly")
            {
                return TDocTokenRole.Keyword;
            }
            if (!_expectsTypedKey && followedByPropertyName)
            {
                _expectsTypedKey = true;
                return TDocTokenRole.Type;
            }
            TakeKey();
            return TDocTokenRole.MapKey;
        }

        return value == "true" || value == "false" || value == "null"
            ? TDocTokenRole.None
            : TDocTokenRole.Type;
    }

    public bool TakeKey()
    {
        var wasKey = _expectsKey;
        _expectsKey = false;
        _expectsTypedKey = false;
        return wasKey;
    }

    public void Punctuation(char value)
    {
        switch (value)
        {
            case '{':
            case '[':
                _containers.Push(value);
                _expectsKey = value == '{';
                break;
            case '}':
            case ']':
                if (_containers.Count != 0) _containers.Pop();
                _expectsKey = false;
                break;
            case ',':
                _expectsKey = _containers.Count != 0 && _containers.Peek() == '{';
                break;
            default:
                return;
        }
        _expectsTypedKey = false;
    }
}

internal enum TDocTokenRole
{
    None,
    Type,
    MapKey,
    Keyword
}
