using System;

namespace AuroraScript.Compiler.Syntax
{
    internal readonly struct SyntaxElement
    {
        private SyntaxElement(SyntaxToken token)
        {
            IsToken = true;
            Token = token;
            Trivia = default;
        }

        private SyntaxElement(SyntaxTrivia trivia)
        {
            IsToken = false;
            Token = default;
            Trivia = trivia;
        }

        public bool IsToken { get; }
        public SyntaxToken Token { get; }
        public SyntaxTrivia Trivia { get; }

        public int Offset => IsToken ? Token.Offset : Trivia.Offset;
        public int Length => IsToken ? Token.Length : Trivia.Length;
        public int StartLine => IsToken ? Token.StartLine : Trivia.StartLine;
        public int StartColumn => IsToken ? Token.StartColumn : Trivia.StartColumn;
        public int EndLine => IsToken ? Token.EndLine : Trivia.EndLine;
        public int EndColumn => IsToken ? Token.EndColumn : Trivia.EndColumn;

        public static SyntaxElement FromToken(SyntaxToken token)
        {
            return new SyntaxElement(token);
        }

        public static SyntaxElement FromTrivia(SyntaxTrivia trivia)
        {
            return new SyntaxElement(trivia);
        }
    }
}
