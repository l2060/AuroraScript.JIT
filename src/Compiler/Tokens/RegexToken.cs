using System;

namespace AuroraScript.Tokens
{
    internal class RegexToken : ValueToken
    {
        internal RegexToken(String literal) : base()
        {
            Literal = literal ?? throw new ArgumentNullException(nameof(literal));
            this.Type = ValueType.Regex;
        }

        private String _pattern;
        private String _flags;

        public String Pattern
        {
            get
            {
                EnsureSplit();
                return _pattern;
            }
        }

        public String Flags
        {
            get
            {
                EnsureSplit();
                return _flags;
            }
        }

        public String Literal { get; }

        public override string ToValue()
        {
            return Literal;
        }

        private void EnsureSplit()
        {
            if (_pattern != null) return;

            (_pattern, _flags) = SplitLiteral(Literal);
        }

        private static (String pattern, String flags) SplitLiteral(String literal)
        {
            if (literal.Length < 2 || literal[0] != '/')
            {
                return (literal, String.Empty);
            }

            var escaped = false;
            var inClass = false;

            for (int i = 1; i < literal.Length; i++)
            {
                var current = literal[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (current == '[')
                {
                    inClass = true;
                    continue;
                }

                if (current == ']' && inClass)
                {
                    inClass = false;
                    continue;
                }

                if (current == '/' && !inClass)
                {
                    var pattern = literal.Substring(1, i - 1);
                    var flags = literal.Substring(i + 1);
                    return (pattern, flags);
                }
            }

            return (literal.Substring(1), String.Empty);
        }
    }
}

