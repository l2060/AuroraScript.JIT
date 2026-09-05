using System;
using System.Text.RegularExpressions;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a regular expression object in AuroraScript.
    /// Wraps the CLI <see cref="Regex"/> to provide pattern matching and replacement capabilities.
    /// </summary>
    public sealed partial class ScriptRegex : ScriptObject
    {
        private readonly Regex _regex;
        private readonly string _flags;
        private readonly string[] _groupNames;

        internal string Pattern => _regex.ToString();
        internal string Flags => _flags ?? string.Empty;

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Regex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptRegex"/> class.
        /// </summary>
        /// <param name="regex">The underlying .NET Regex object.</param>
        /// <param name="flags">The regex flags (e.g., "g", "i", "m").</param>
        public ScriptRegex(Regex regex, string flags) : base(Prototypes.RegexPrototype)
        {
            _regex = regex;
            _flags = flags;
            _groupNames = regex.GetGroupNames();
        }

        /// <summary>
        /// Tests if the provided datum (as a string) matches the regular expression.
        /// </summary>
        /// <param name="value">The datum to test.</param>
        /// <returns>True if a match is found; otherwise, false.</returns>
        public bool Test(ScriptDatum value)
        {
            if (value.Kind == ValueKind.String)
            {
                var result = _regex.Match(value.StringText);
                return result.Success;
            }
            return false;
        }

        /// <summary>
        /// Executes a single match against the provided string.
        /// </summary>
        /// <param name="str">The string to match.</param>
        /// <returns>A match result array or <see cref="ScriptObject.Null"/> if no match.</returns>
        public ScriptObject Match(StringValue str)
            => str == null ? ScriptObject.Null : MatchText(str.Value);

        /// <summary>Raw string path shared by native and dynamic String members.</summary>
        internal ScriptObject MatchText(string value)
        {
            var input = value ?? string.Empty;
            var match = _regex.Match(input);
            if (!match.Success)
            {
                return ScriptObject.Null;
            }

            return CreateMatchResult(match, input);
        }

        /// <summary>
        /// Executes a global match, returning all matching substrings as an array of strings.
        /// </summary>
        /// <param name="str">The string to match.</param>
        /// <returns>A <see cref="ScriptArray"/> of matching strings or <see cref="ScriptObject.Null"/>.</returns>
        public ScriptObject MatchOfGlobal(StringValue str)
            => str == null ? ScriptObject.Null : MatchOfGlobalText(str.Value);

        internal ScriptObject MatchOfGlobalText(string value)
        {
            var input = value ?? string.Empty;
            var matches = _regex.Matches(input);
            if (matches.Count == 0)
            {
                return ScriptObject.Null;
            }
            var result = ScriptArray.CreateWithCapacity(matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                result.SetElement(i, ScriptDatum.FromString(matches[i].Value));
            }
            return result;
        }

        /// <summary>
        /// Executes the 'matchAll' operation, returning detailed match results for all matches.
        /// </summary>
        /// <param name="str">The string to match.</param>
        /// <returns>A <see cref="ScriptArray"/> of detailed match results or null.</returns>
        public ScriptArray MatchAll(StringValue str)
            => str == null ? null : MatchAllText(str.Value);

        internal ScriptArray MatchAllText(string value)
        {
            var input = value ?? string.Empty;
            var matches = _regex.Matches(input);
            if (matches.Count == 0)
            {
                return null;
            }

            var outer = ScriptArray.CreateWithCapacity(matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                var matchResult = CreateMatchResult(matches[i], input);
                outer.SetElement(i, ScriptDatum.FromObject(matchResult));
            }
            return outer;
        }

        /// <summary> Checks if the specified flag is set for this regular expression. </summary>
        public bool HasFlag(string flag)
        {
            return _flags.IndexOf(flag, StringComparison.Ordinal) > -1;
        }

        /// <summary> Internal helper for string replacement using this regex. </summary>
        internal string Replace(string input, string replacement, bool replaceAll)
        {
            input ??= string.Empty;
            replacement ??= string.Empty;
            var count = replaceAll ? int.MaxValue : 1;
            return _regex.Replace(input, replacement, count);
        }

        /// <summary> Internal helper for string replacement using a callback evaluator. </summary>
        internal string Replace(string input, MatchEvaluator evaluator, bool replaceAll)
        {
            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            input ??= string.Empty;
            var count = replaceAll ? int.MaxValue : 1;
            return _regex.Replace(input, evaluator, count);
        }

        /// <summary> Native implementation for the 'test' method. </summary>
        public static void TEST(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetRef(0, ref result))
            {
                var regex = thisObject as ScriptRegex;
                ScriptDatum.WriteAsBoolean(ref result, regex.Test(result));
            }
            else
            {
                ScriptDatum.WriteAsBoolean(ref result, false);
            }
        }

        /// <summary> Returns the string representation of the regular expression pattern. </summary>
        public override string ToString()
        {
            return _regex.ToString();
        }

        /// <summary> Creates a JS-compliant match result object containing capture groups, index, and input. </summary>
        private ScriptArray CreateMatchResult(Match match, string input)
        {
            var groupCount = match.Groups.Count;
            var result = ScriptArray.CreateWithCapacity(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                result.SetElement(i, ScriptDatum.FromString(match.Groups[i].Value));
            }

            result.SetPropertyValue("index", NumberValue.Of(match.Index));
            result.SetPropertyValue("input", StringValue.Of(input ?? string.Empty));

            ScriptObject namedGroups = null;
            for (int i = 0; i < _groupNames.Length; i++)
            {
                var name = _groupNames[i];
                if (string.IsNullOrEmpty(name) || int.TryParse(name, out _))
                {
                    continue;
                }

                namedGroups ??= new ScriptObject();
                var capture = match.Groups[name];
                if (capture.Success)
                {
                    namedGroups.SetPropertyValue(name, StringValue.Of(capture.Value));
                }
                else
                {
                    namedGroups.SetPropertyValue(name, ScriptObject.Null);
                }
            }

            if (namedGroups != null)
            {
                result.SetPropertyValue("groups", namedGroups);
            }

            return result;
        }
    }
}
