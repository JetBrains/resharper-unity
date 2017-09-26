using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing
{
    /// <summary>
    /// This is only used to not get errors from PP directives
    /// </summary>
    internal class CgPreProcessor
    {
        private readonly Dictionary<int, TreeElement> myPpDirectivesByOffset = new Dictionary<int, TreeElement>();
        private readonly List<TextRange> myRanges = new List<TextRange>();

        public TreeElement GetPpDirectiveAtOffset(int startOffset)
        {
            return myPpDirectivesByOffset.TryGetValue(startOffset, out var element) ? element : null;
        }

        public bool IsInPpTokenRange(int startOffset)
        {
            return myRanges.BinarySearch(new TextRange(startOffset), TextRangeSearchingComparer.Instance) >= 0;
        }

        public void Run(ILexer<int> lexer, CgParser parser, SeldomInterruptChecker interruptChecker)
        {
            for (var tokenType = lexer.TokenType; tokenType != null; tokenType = lexer.TokenType)
            {
                interruptChecker.CheckForInterrupt();

                if (tokenType == CgTokenNodeTypes.DIRECTIVE)
                {
                    var startOffset = lexer.TokenStart;
                    var directiveElement = parser.ParseDirective();
                    myPpDirectivesByOffset[startOffset] = directiveElement;
                    myRanges.Add(new TextRange(startOffset, lexer.TokenStart));
                }
                else
                {
                    lexer.Advance();
                }
            }
        }

        private class TextRangeSearchingComparer : IComparer<TextRange>
        {
            [NotNull]
            public static readonly TextRangeSearchingComparer Instance = new TextRangeSearchingComparer();

            public int Compare(TextRange x, TextRange y)
            {
                int startOffset = y.StartOffset;
                if (x.EndOffset <= startOffset)
                    return -1;
                return x.StartOffset > startOffset ? 1 : 0;
            }
        }
    }
}