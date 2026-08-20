using AuroraScript.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AuroraScript.Compiler
{
    internal class Symbols
    {
        // key words

        public static readonly Symbols KW_DECLARE = new Symbols("declare", SymbolTypes.KeyWord);
        public static readonly Symbols KW_IF = new Symbols("if", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_AS = new Symbols("as", SymbolTypes.KeyWord);
        public static readonly Symbols KW_ELSE = new Symbols("else", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_TYPE = new Symbols("type", SymbolTypes.KeyWord);
        public static readonly Symbols KW_CONST = new Symbols("const", SymbolTypes.KeyWord);
        public static readonly Symbols KW_FUNCTION = new Symbols("function", SymbolTypes.KeyWord);
        public static readonly Symbols KW_FUNC = new Symbols("func", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_GET = new Symbols("get", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_SET = new Symbols("set", SymbolTypes.KeyWord);

        public static readonly Symbols KW_VAR = new Symbols("var", SymbolTypes.KeyWord);
        public static readonly Symbols KW_RETURN = new Symbols("return", SymbolTypes.KeyWord);
        public static readonly Symbols KW_DEBUGGER = new Symbols("debugger", SymbolTypes.KeyWord);

        public static readonly Symbols KW_BREAK = new Symbols("break", SymbolTypes.KeyWord);
        public static readonly Symbols KW_CONTINUE = new Symbols("continue", SymbolTypes.KeyWord);
        public static readonly Symbols KW_ENUM = new Symbols("enum", SymbolTypes.KeyWord);
        public static readonly Symbols KW_FOR = new Symbols("for", SymbolTypes.KeyWord);
        public static readonly Symbols KW_NEW = new Symbols("new", SymbolTypes.KeyWord);
        public static readonly Symbols KW_DELETE = new Symbols("delete", SymbolTypes.KeyWord);
        public static readonly Symbols KW_TDOC = new Symbols("tdoc", SymbolTypes.KeyWord);


        public static readonly Symbols KW_WHILE = new Symbols("while", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_PRIVATE = new Symbols("private", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_PROTECTED = new Symbols("protected", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_PUBLIC = new Symbols("public", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_STATIC = new Symbols("static", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_CLASS = new Symbols("class", SymbolTypes.KeyWord);

        public static readonly Symbols KW_TRY = new Symbols("try", SymbolTypes.KeyWord);
        public static readonly Symbols KW_CATCH = new Symbols("catch", SymbolTypes.KeyWord);
        public static readonly Symbols KW_FINALLY = new Symbols("finally", SymbolTypes.KeyWord);
        public static readonly Symbols KW_THROW = new Symbols("throw", SymbolTypes.KeyWord);





        public static readonly Symbols KW_IMPORT = new Symbols("import", SymbolTypes.KeyWord);
        public static readonly Symbols KW_INCLUDE = new Symbols("include", SymbolTypes.KeyWord);




        public static readonly Symbols KW_FROM = new Symbols("from", SymbolTypes.KeyWord);
        public static readonly Symbols KW_EXPORT = new Symbols("export", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_SEALED = new Symbols("sealed", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_INTERNAL = new Symbols("internal", SymbolTypes.KeyWord);

        //public static readonly Symbols KW_EXTENDS = new Symbols("extends", SymbolTypes.KeyWord);
        //public static readonly Symbols KW_IMPLEMENTS = new Symbols("implements", SymbolTypes.KeyWord);


        // types
        //public static readonly Symbols KW_THIS = new Symbols("this", SymbolTypes.Identifier);

        // byte char short ushort long ulong float double
        // number = double
        //public static readonly Symbols TYPED_NUMBER = new Symbols("number", SymbolTypes.Identifier);


        /// <summary>
        /// token typeof
        /// </summary>
        public static readonly Symbols OP_TYPEOF = new Symbols("typeof", SymbolTypes.Punctuator);


        // Punctuator
        /// <summary>
        /// token {
        /// </summary>
        public static readonly Symbols PT_METAINFO = new Symbols("@", SymbolTypes.Punctuator);



        // Punctuator
        /// <summary>
        /// token {
        /// </summary>
        public static readonly Symbols PT_LEFTBRACE = new Symbols("{", SymbolTypes.Punctuator);

        /// <summary>
        /// token }
        /// </summary>
        public static readonly Symbols PT_RIGHTBRACE = new Symbols("}", SymbolTypes.Punctuator);

        /// <summary>
        /// token (
        /// </summary>
        public static readonly Symbols PT_LEFTPARENTHESIS = new Symbols("(", SymbolTypes.Punctuator);

        /// <summary>
        /// token )
        /// </summary>
        public static readonly Symbols PT_RIGHTPARENTHESIS = new Symbols(")", SymbolTypes.Punctuator);

        /// <summary>
        /// token [
        /// </summary>
        public static readonly Symbols PT_LEFTBRACKET = new Symbols("[", SymbolTypes.Punctuator);

        /// <summary>
        /// token ]
        /// </summary>
        public static readonly Symbols PT_RIGHTBRACKET = new Symbols("]", SymbolTypes.Punctuator);

        /// <summary>
        /// token ;
        /// </summary>
        public static readonly Symbols PT_SEMICOLON = new Symbols(";", SymbolTypes.Punctuator);

        /// <summary>
        /// token ,
        /// </summary>
        public static readonly Symbols PT_COMMA = new Symbols(",", SymbolTypes.Punctuator);

        /// <summary>
        /// token .
        /// </summary>
        public static readonly Symbols PT_DOT = new Symbols(".", SymbolTypes.Punctuator);

        /// <summary>
        /// token :
        /// </summary>
        public static readonly Symbols PT_COLON = new Symbols(":", SymbolTypes.Punctuator);

        /// <summary>
        /// token =>
        /// </summary>
        public static readonly Symbols PT_LAMBDA = new Symbols("=>", SymbolTypes.Operator);

        // Operators
        /// <summary>
        /// token &lt;
        /// </summary>
        public static readonly Symbols OP_LESSTHAN = new Symbols("<", SymbolTypes.Operator);

        /// <summary>
        /// token &gt;
        /// </summary>
        public static readonly Symbols OP_GREATERTHAN = new Symbols(">", SymbolTypes.Operator);

        /// <summary>
        /// token &lt;=
        /// </summary>
        public static readonly Symbols OP_LESS_EQUAL = new Symbols("<=", SymbolTypes.Operator);

        /// <summary>
        /// token &gt;=
        /// </summary>
        public static readonly Symbols OP_GREATER_EQUAL = new Symbols(">=", SymbolTypes.Operator);

        /// <summary>
        /// token ==
        /// </summary>
        public static readonly Symbols OP_EQUAL = new Symbols("==", SymbolTypes.Operator);

        /// <summary>
        /// token !=
        /// </summary>
        public static readonly Symbols OP_NOT_EQUAL = new Symbols("!=", SymbolTypes.Operator);

        /// <summary>
        /// token +
        /// </summary>
        public static readonly Symbols OP_PLUS = new Symbols("+", SymbolTypes.Operator);

        /// <summary>
        /// token -
        /// </summary>
        public static readonly Symbols OP_SUBTRACT = new Symbols("-", SymbolTypes.Operator);

        /// <summary>
        /// token *
        /// </summary>
        public static readonly Symbols OP_MULTIPLY = new Symbols("*", SymbolTypes.Operator);

        /// <summary>
        /// token /
        /// </summary>
        public static readonly Symbols OP_DIVIDE = new Symbols("/", SymbolTypes.Operator);

        /// <summary>
        /// token %
        /// </summary>
        public static readonly Symbols OP_MODULO = new Symbols("%", SymbolTypes.Operator);

        /// <summary>
        /// token ...
        /// </summary>
        public static readonly Symbols OP_SPREAD = new Symbols("...", SymbolTypes.Operator);

        /// <summary>
        /// token ++
        /// </summary>
        public static readonly Symbols OP_INCREMENT = new Symbols("++", SymbolTypes.Operator);

        /// <summary>
        /// token --
        /// </summary>
        public static readonly Symbols OP_DECREMENT = new Symbols("--", SymbolTypes.Operator);

        /// <summary>
        /// token &lt;&lt;
        /// </summary>
        public static readonly Symbols OP_LEFTSHIFT = new Symbols("<<", SymbolTypes.Operator);

        /// <summary>
        /// token >>
        /// </summary>
        public static readonly Symbols OP_SIGNEDRIGHTSHIFT = new Symbols(">>", SymbolTypes.Operator);

        /// <summary>
        /// token >>
        /// </summary>
        public static readonly Symbols OP_UNSIGNEDRIGHTSHIFT = new Symbols(">>>", SymbolTypes.Operator);

        public static readonly Symbols OP_IN = new Symbols("in", SymbolTypes.Operator);

        /// <summary>
        /// token &amp;
        /// </summary>
        public static readonly Symbols OP_BIT_AND = new Symbols("&", SymbolTypes.Operator);

        /// <summary>
        /// token |
        /// </summary>
        public static readonly Symbols OP_BIT_OR = new Symbols("|", SymbolTypes.Operator);

        /// <summary>
        /// token ^
        /// </summary>
        public static readonly Symbols OP_BIT_XOR = new Symbols("^", SymbolTypes.Operator);

        /// <summary>
        /// token !
        /// </summary>
        public static readonly Symbols OP_LOGICALNOT = new Symbols("!", SymbolTypes.Operator);

        /// <summary>
        /// token ~
        /// </summary>
        public static readonly Symbols OP_BIT_NOT = new Symbols("~", SymbolTypes.Operator);

        /// <summary>
        /// token &amp;&amp;
        /// </summary>
        public static readonly Symbols OP_LOGICAL_AND = new Symbols("&&", SymbolTypes.Operator);

        /// <summary>
        /// token ||
        /// </summary>
        public static readonly Symbols OP_LOGICAL_OR = new Symbols("||", SymbolTypes.Operator);

        /// <summary>
        /// token ?
        /// </summary>
        public static readonly Symbols OP_CONDITIONAL = new Symbols("?", SymbolTypes.Operator);

        /// <summary>
        /// token =
        /// </summary>
        public static readonly Symbols OP_ASSIGNMENT = new Symbols("=", SymbolTypes.Operator);

        /// <summary>
        /// token +=
        /// </summary>
        public static readonly Symbols OP_COMPOUNDADD = new Symbols("+=", SymbolTypes.Operator);

        /// <summary>
        /// token -=
        /// </summary>
        public static readonly Symbols OP_COMPOUNDSUBTRACT = new Symbols("-=", SymbolTypes.Operator);

        /// <summary>
        /// token *=
        /// </summary>
        public static readonly Symbols OP_COMPOUNDMULTIPLY = new Symbols("*=", SymbolTypes.Operator);

        /// <summary>
        /// token /=
        /// </summary>
        public static readonly Symbols OP_COMPOUNDDIVIDE = new Symbols("/=", SymbolTypes.Operator);

        /// <summary>
        /// token %=
        /// </summary>
        public static readonly Symbols OP_COMPOUNDMODULO = new Symbols("%=", SymbolTypes.Operator);

        /// <summary>
        /// token End of File
        /// </summary>
        public static readonly Symbols KW_EOF = new Symbols("END OF FILE", SymbolTypes.Operator);

        /// <summary>
        /// token true
        /// </summary>
        public static readonly Symbols VALUE_TRUE = new Symbols("true", SymbolTypes.BooleanValue);

        /// <summary>
        /// token false
        /// </summary>
        public static readonly Symbols VALUE_FALSE = new Symbols("false", SymbolTypes.BooleanValue);

        /// <summary>
        /// token null
        /// </summary>
        public static readonly Symbols VALUE_NULL = new Symbols("null", SymbolTypes.NullValue);

        private static readonly Dictionary<string, Symbols> _SymbolMaps = new Dictionary<string, Symbols>();
        private static Symbols[] _SymbolsById;

        static Symbols()
        {
            var type = typeof(Symbols);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(Symbols));
            var id = 0;
            foreach (var field in fields)
            {
                var symbol = field.GetValue(null) as Symbols;
                symbol.Id = id++;
                _SymbolMaps.Add(symbol.Name, symbol);
            }
            _SymbolsById = new Symbols[id];
            foreach (var symbol in _SymbolMaps.Values)
            {
                _SymbolsById[symbol.Id] = symbol;
            }
        }

        internal static int Count => _SymbolsById.Length;

        internal int Id { get; private set; }

        internal static Symbols FromId(int id)
        {
            return id >= 0 && id < _SymbolsById.Length ? _SymbolsById[id] : null;
        }

        /// <summary>
        /// prase symbol from string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Symbols FromString(string name)
        {
            _SymbolMaps.TryGetValue(name, out var symbol);
            return symbol;
        }

        internal static Symbols FromSpan(ReadOnlySpan<char> name)
        {
            switch (name.Length)
            {
                case 1:
                    switch (name[0])
                    {
                        case '@': return PT_METAINFO;
                        case '{': return PT_LEFTBRACE;
                        case '}': return PT_RIGHTBRACE;
                        case '(': return PT_LEFTPARENTHESIS;
                        case ')': return PT_RIGHTPARENTHESIS;
                        case '[': return PT_LEFTBRACKET;
                        case ']': return PT_RIGHTBRACKET;
                        case ';': return PT_SEMICOLON;
                        case ',': return PT_COMMA;
                        case '.': return PT_DOT;
                        case ':': return PT_COLON;
                        case '<': return OP_LESSTHAN;
                        case '>': return OP_GREATERTHAN;
                        case '+': return OP_PLUS;
                        case '-': return OP_SUBTRACT;
                        case '*': return OP_MULTIPLY;
                        case '/': return OP_DIVIDE;
                        case '%': return OP_MODULO;
                        case '&': return OP_BIT_AND;
                        case '|': return OP_BIT_OR;
                        case '^': return OP_BIT_XOR;
                        case '!': return OP_LOGICALNOT;
                        case '~': return OP_BIT_NOT;
                        case '?': return OP_CONDITIONAL;
                        case '=': return OP_ASSIGNMENT;
                    }
                    break;
                case 2:
                    switch (name[0])
                    {
                        case '<': return name[1] == '=' ? OP_LESS_EQUAL : name[1] == '<' ? OP_LEFTSHIFT : null;
                        case '>': return name[1] == '=' ? OP_GREATER_EQUAL : name[1] == '>' ? OP_SIGNEDRIGHTSHIFT : null;
                        case '=': return name[1] == '=' ? OP_EQUAL : name[1] == '>' ? PT_LAMBDA : null;
                        case '!': return name[1] == '=' ? OP_NOT_EQUAL : null;
                        case '+': return name[1] == '+' ? OP_INCREMENT : name[1] == '=' ? OP_COMPOUNDADD : null;
                        case '-': return name[1] == '-' ? OP_DECREMENT : name[1] == '=' ? OP_COMPOUNDSUBTRACT : null;
                        case '*': return name[1] == '=' ? OP_COMPOUNDMULTIPLY : null;
                        case '/': return name[1] == '=' ? OP_COMPOUNDDIVIDE : null;
                        case '%': return name[1] == '=' ? OP_COMPOUNDMODULO : null;
                        case '&': return name[1] == '&' ? OP_LOGICAL_AND : null;
                        case '|': return name[1] == '|' ? OP_LOGICAL_OR : null;
                        case 'i': return name[1] == 'f' ? KW_IF : name[1] == 'n' ? OP_IN : null;
                    }
                    break;
                case 3:
                    if (name[0] == '.' && name[1] == '.' && name[2] == '.') return OP_SPREAD;
                    if (name[0] == '>' && name[1] == '>' && name[2] == '>') return OP_UNSIGNEDRIGHTSHIFT;
                    if (name.SequenceEqual("var")) return KW_VAR;
                    if (name.SequenceEqual("for")) return KW_FOR;
                    if (name.SequenceEqual("new")) return KW_NEW;
                    if (name.SequenceEqual("try")) return KW_TRY;
                    break;
                case 4:
                    if (name.SequenceEqual("else")) return KW_ELSE;
                    if (name.SequenceEqual("enum")) return KW_ENUM;
                    if (name.SequenceEqual("from")) return KW_FROM;
                    if (name.SequenceEqual("func")) return KW_FUNC;
                    if (name.SequenceEqual("tdoc")) return KW_TDOC;
                    if (name.SequenceEqual("true")) return VALUE_TRUE;
                    if (name.SequenceEqual("null")) return VALUE_NULL;
                    break;
                case 5:
                    if (name.SequenceEqual("const")) return KW_CONST;
                    if (name.SequenceEqual("while")) return KW_WHILE;
                    if (name.SequenceEqual("break")) return KW_BREAK;
                    if (name.SequenceEqual("catch")) return KW_CATCH;
                    if (name.SequenceEqual("throw")) return KW_THROW;
                    if (name.SequenceEqual("false")) return VALUE_FALSE;
                    break;
                case 6:
                    if (name.SequenceEqual("return")) return KW_RETURN;
                    if (name.SequenceEqual("delete")) return KW_DELETE;
                    if (name.SequenceEqual("import")) return KW_IMPORT;
                    if (name.SequenceEqual("export")) return KW_EXPORT;
                    if (name.SequenceEqual("typeof")) return OP_TYPEOF;
                    break;
                case 7:
                    if (name.SequenceEqual("declare")) return KW_DECLARE;
                    if (name.SequenceEqual("include")) return KW_INCLUDE;
                    if (name.SequenceEqual("finally")) return KW_FINALLY;
                    break;
                case 8:
                    if (name.SequenceEqual("function")) return KW_FUNCTION;
                    if (name.SequenceEqual("debugger")) return KW_DEBUGGER;
                    if (name.SequenceEqual("continue")) return KW_CONTINUE;
                    break;
            }
            return null;
        }

        /// <summary>
        /// get symbol name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// get symbol type
        /// </summary>
        internal SymbolTypes Type { get; private set; }

        private Symbols(string name, SymbolTypes type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Name}:{Type}";
        }
    }
}
