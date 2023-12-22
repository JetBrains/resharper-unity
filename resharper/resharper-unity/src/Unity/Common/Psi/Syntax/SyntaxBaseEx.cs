#nullable enable
using System.Diagnostics.CodeAnalysis;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Syntax
{
    public static class SyntaxBaseEx
    {
        /// <summary>Checks if line containing <paramref name="originToken"/> is blank (is empty or contains only whitespace tokens).</summary>
        public static bool IsBlankLine(this SyntaxBase syntax, ITokenNode originToken) => !TryGetSingleNonWhitespaceTokenOnLine(syntax, originToken, out var nonWhitespace) && nonWhitespace == null; 
        
        /// <summary>
        /// Tries to get single non-whitespace token on the line starting from <paramref name="originToken"/>.
        /// If there exactly a single token then outputs it in <paramref name="nonWhitespaceToken"/> and returns <c>true</c>.
        /// If there more than one non-whitespace token then outputs random token in <paramref name="nonWhitespaceToken"/> and returns <c>false</c>.
        /// If there none non-whitespace tokens then outputs <c>null</c> in <paramref name="nonWhitespaceToken"/> and returns <c>false</c>.
        /// </summary>
        public static bool TryGetSingleNonWhitespaceTokenOnLine(this SyntaxBase syntax, ITokenNode originToken, [MaybeNullWhen(false)] out ITokenNode nonWhitespaceToken)
        {
            var tt = originToken.GetTokenType();
            nonWhitespaceToken = tt != syntax.WHITE_SPACE && tt != syntax.NEW_LINE ? originToken : null;   
            foreach (var token in new PrevTokensUntilTerminalTokenEnumerator(originToken, syntax.NEW_LINE))
            {
                if (token.GetTokenType() == syntax.WHITE_SPACE)
                    continue;
                if (nonWhitespaceToken != null)
                    return false;
                nonWhitespaceToken = token;
            }

            if (tt != syntax.NEW_LINE)
            {
                foreach (var token in new NextTokensUntilTerminalTokenEnumerator(originToken, syntax.NEW_LINE))
                {
                    if (token.GetTokenType() == syntax.WHITE_SPACE)
                        continue;
                    if (nonWhitespaceToken != null)
                        return false;
                    nonWhitespaceToken = token;
                }
            }

            return nonWhitespaceToken != null;
        }
        
        public struct PrevTokensUntilTerminalTokenEnumerator
        {
            private readonly ITokenNodeType? myTerminalTokenType;
            
            private ITokenNode myCurrent;

            public PrevTokensUntilTerminalTokenEnumerator(ITokenNode originToken, ITokenNodeType? terminalTokenType)
            {
                myCurrent = originToken;
                myTerminalTokenType = terminalTokenType;
            }
            
            public ITokenNode Current => myCurrent;

            public bool MoveNext()
            {
                var prev = myCurrent.GetPreviousToken();
                if (prev == null || ReferenceEquals(prev.GetTokenType(), myTerminalTokenType)) 
                    return false;
                myCurrent = prev;
                return true;
            }
            
            public PrevTokensUntilTerminalTokenEnumerator GetEnumerator() => this;
        }
        
        public struct NextTokensUntilTerminalTokenEnumerator
        {
            private readonly ITokenNodeType myTerminalTokenType;
            
            private ITokenNode myCurrent;

            public NextTokensUntilTerminalTokenEnumerator(ITokenNode originToken, ITokenNodeType terminalTokenType)
            {
                myCurrent = originToken;
                myTerminalTokenType = terminalTokenType;
            }

            public ITokenNode Current => myCurrent;

            public bool MoveNext()
            {
                var next = myCurrent.GetNextToken();
                if (next == null || ReferenceEquals(next.GetTokenType(), myTerminalTokenType)) 
                    return false;
                myCurrent = next;
                return true;
            }
            
            public NextTokensUntilTerminalTokenEnumerator GetEnumerator() => this;
        }
    }
}