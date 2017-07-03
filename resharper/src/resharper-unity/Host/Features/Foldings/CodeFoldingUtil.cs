#if RIDER

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Host.Features.Foldings
{
    // TODO: Delete this once we have a Rider SDK and can just use ICodeFoldingProcess
    internal static class CodeFoldingUtil
    {
        private const int MaxRemainingCommentTextLength = 110;

        [Pure]
        public static bool IsNotEmptyNormalized(this DocumentRange range)
        {
            var textRange = range.TextRange;
            return range.IsValid() && (textRange.StartOffset < textRange.EndOffset);
        }

        public static void AddDefaultPriorityFolding(this IHighlightingConsumer context, string attributeId,  DocumentRange range, string placeholder)
        {
            const int DEFAULT_FOLDING_PRIORITY = 10;
            var codeFoldingHighlighting = CodeFoldingHighlightingCreator.Create(attributeId, placeholder, range, DEFAULT_FOLDING_PRIORITY);
            if (codeFoldingHighlighting != null)
                context.AddHighlighting(codeFoldingHighlighting);
        }

        public static bool AddFoldingForBracedConstruct(
            [NotNull] this IHighlightingConsumer context,
            [CanBeNull] ITokenNode lbrace, [CanBeNull] ITokenNode rbrace, [CanBeNull] ITreeNode anchorToken = null,
            string placeholder = "{...}")
        {
            if (lbrace == null || rbrace == null) return false;

            var start = lbrace.GetDocumentStartOffset();
            var end = rbrace.GetDocumentEndOffset();
            var anchorOffset = anchorToken?.GetDocumentEndOffset().Offset ?? -1;

            var document = start.Document;
            var endDocument = end.Document;
            if (document == null || end < start || document != endDocument) return false;

            var foldingRange = new DocumentRange(start, end);
            if (anchorOffset != -1 && anchorOffset < start.Offset)
            {
                var offset = document.FindLastNewLineOffset(anchorOffset, start.Offset);
                foldingRange = foldingRange.SetStartTo(new DocumentOffset(document, offset));
            }

            if (!foldingRange.IsNotEmptyNormalized()) return false;
            if (foldingRange.CountNewLines() < 1) return false;

            context.AddDefaultPriorityFolding("ReSharper Default Folding", foldingRange, placeholder);
            return true;
        }

        public static void AddFoldingForCLikeCommentTokens(this IHighlightingConsumer context, ITreeNode node, [CanBeNull] TokenNodeType lineComment,
            [CanBeNull] TokenNodeType cStyleComment, [NotNull] TokenNodeType newLine)
        {
            var token = node as ITokenNode;
            if (token == null) return;
            var tokenNodeType = token.GetTokenType();

            if (lineComment != null && tokenNodeType == lineComment)
            {
                var rightNode = token.SkipRightWhitespaces(newLine, 2);
                if (rightNode != null && rightNode.GetTokenType() == lineComment) return;
                var range = GetRangeForListOfSimilarTokenNodeTypes(node, lineComment, newLine, 2);
                var countNewLines = range.CountNewLines();
                if (countNewLines < 1 || !range.IsNotEmptyNormalized()) return;
                context.AddDefaultPriorityFolding("ReSharper Comments Folding", range, CalculateCLikeCommentPresentation(range, countNewLines));
            }

            if (cStyleComment != null && tokenNodeType == cStyleComment)
            {
                var range = token.GetDocumentRange();
                var countNewLines = range.CountNewLines();
                if (countNewLines < 1 || !range.IsNotEmptyNormalized()) return;
                context.AddDefaultPriorityFolding("ReSharper Comments Folding", range, CalculateCLikeCommentPresentation(range, countNewLines));
            }
        }

        [Pure]
        private static int FindLastNewLineOffset(this IDocument document, int start, int end)
        {
            if (start >= end) return end;
            if (document == null) return end;
            var range = new TextRange(start, end);
            if (!range.ContainedIn(document.DocumentRange)) return 0;
            var text = document.GetText(range);
            if (text == null) return 0;
            var lastNewLine = text.LastIndexOfAny(StringUtil.NEW_LINE_CHARACTERS);
            return lastNewLine == -1 ? end : start + lastNewLine;
        }

        private static DocumentRange GetRangeForListOfSimilarTokenNodeTypes(ITreeNode lastNode, TokenNodeType sampleTokenType, TokenNodeType newLineToken, int newLineLimit)
        {
            var range = lastNode.GetDocumentRange();
            var currentNode = lastNode.SkipLeftWhitespaces(newLineToken, newLineLimit);
            while (true)
            {
                if (currentNode == null || currentNode.GetTokenType() != sampleTokenType) break;

                range = range.SetStartTo(currentNode.GetDocumentStartOffset());
                currentNode = currentNode.SkipLeftWhitespaces(newLineToken, newLineLimit);
            }

            return range;
        }

        [CanBeNull]
        private static ITreeNode SkipLeftWhitespaces([NotNull] this ITreeNode tokenNode, TokenNodeType newLineToken, int newLineLimit = -1)
        {
            return SkipWhiteSpacesInternal(tokenNode, newLineToken, true, newLineLimit);
        }

        [CanBeNull]
        private static ITreeNode SkipRightWhitespaces([NotNull] this ITreeNode tokenNode, TokenNodeType newLineToken, int newLineLimit = -1)
        {
            return SkipWhiteSpacesInternal(tokenNode, newLineToken, false, newLineLimit);
        }

        [CanBeNull]
        private static ITreeNode SkipWhiteSpacesInternal([NotNull] this ITreeNode tokenNode, TokenNodeType newLineToken, bool searchToLeft, int newLineLimit = -1)
        {
            var counter = 0;
            tokenNode = searchToLeft ? tokenNode.PrevSibling : tokenNode.NextSibling;

            while (true)
            {
                if (tokenNode == null) break;
                var tokenNodeType = tokenNode.GetTokenType();
                if (tokenNodeType == newLineToken && counter++ >= newLineLimit) break;
                if (!tokenNode.IsWhitespaceToken()) break;
                tokenNode = searchToLeft ? tokenNode.PrevSibling : tokenNode.NextSibling;
            }
            return tokenNode;
        }

        [Pure]
        private static int CountNewLines(this DocumentRange range)
        {
            return CountNewLines(range.Document, range.TextRange);
        }

        [Pure]
        private static int CountNewLines(this IDocument document, TextRange range)
        {
            var result = 0;
            if (document == null) return 0;
            if (!range.ContainedIn(document.DocumentRange)) return 0;
            var text = document.GetText(range);
            if (text == null) return 0;
            foreach (var newLine in StringUtil.NEW_LINE_CHARACTERS)
            {
                result += StringUtil.Count(text, newLine);
            }
            return result;
        }

        private static string CalculateCLikeCommentPresentation(DocumentRange range, int countNewLines,
            string prefix = "/*", string postfix = "*/")
        {
            var text = range.GetText();

            var newLineIndex = -1;
            var firstLine = string.Empty;
            while (true)
            {
                var prevIndex = newLineIndex;
                if (prevIndex + 1 >= text.Length) break;
                newLineIndex = text.IndexOfAny(StringUtil.NEW_LINE_CHARACTERS, prevIndex + 1);

                if (prevIndex > 0 && newLineIndex == -1)
                    newLineIndex = text.Length;
                else if (newLineIndex >= text.Length || newLineIndex == -1)
                    break;

                var line = text.Substring(prevIndex + 1, newLineIndex - prevIndex - 1);
                var lineLength = line.Length;

                int shiftStart;
                for (shiftStart = 0; shiftStart < lineLength; shiftStart++)
                {
                    var character = line[shiftStart];
                    if (char.IsWhiteSpace(character) || character == '/' || character == '*')
                        continue;
                    break;
                }

                if (shiftStart == lineLength)
                    continue;

                var shiftEnd = 0;
                for (var i = lineLength - 1; i >= 0; i--)
                {
                    var character = line[i];
                    if (char.IsWhiteSpace(character) || character == '/' || character == '*')
                    {
                        shiftEnd++;
                        continue;
                    }
                    break;
                }

                var start = shiftStart;
                if (start >= lineLength || lineLength - shiftStart - shiftEnd < 0) continue;
                var sub = line.Substring(shiftStart, lineLength - shiftStart - shiftEnd).Trim();
                if (sub.IsEmpty()) continue;
                if (sub.Length > MaxRemainingCommentTextLength)
                    sub = sub.Substring(0, MaxRemainingCommentTextLength);

                firstLine = " " + sub;
                break;
            }

            return prefix + firstLine + " ... " + postfix;
        }

        internal static IList<HighlightingInfo> AppendRangeWithOverlappingResolve(IList<HighlightingInfo> range)
        {
            var result = new List<HighlightingInfo>();
            // Can be done in one loop if needed, but a lot less readable
            // Sort by folding-priority and range
            range.StableSort(FoldingComparer.Instance);
            foreach (var h in range)
                InsertFolding(result, h);
            return result;
        }

        private static void InsertFolding(IList<HighlightingInfo> result, HighlightingInfo highlightingInfo)
        {
            var textRange = highlightingInfo.Highlighting.CalculateRange().TextRange;
            var start = textRange.StartOffset;
            var end = textRange.EndOffset;

            var insertIndex = result.Count;
            for (var i = 0; i < result.Count; i++)
            {
                var range = result[i].Highlighting.CalculateRange().TextRange;
                var rStart = range.StartOffset;
                var rEnd = range.EndOffset;
                if (rStart < start)
                {
                    if (start < rEnd && rEnd < end)
                        return;
                }
                else if (rStart == start)
                {
                    if (rEnd == end)
                        return;
                    if (rEnd > end)
                        insertIndex = Math.Min(insertIndex, i);
                }
                else
                {
                    insertIndex = Math.Min(insertIndex, i);
                    if (rStart > end) break;
                    if (rStart < end && end < rEnd)
                        return;
                }
            }
            result.Insert(insertIndex, highlightingInfo);
        }

        private class FoldingComparer : IComparer<HighlightingInfo>
        {
            public static readonly FoldingComparer Instance = new FoldingComparer();

            public int Compare(HighlightingInfo x, HighlightingInfo y)
            {
                var startOffset = x.Range.TextRange.StartOffset.CompareTo(y.Range.TextRange.StartOffset);
                if (startOffset != 0) return startOffset;
                var endOffset = x.Range.TextRange.EndOffset.CompareTo(y.Range.TextRange.EndOffset);
                return endOffset;
            }
        }

        private static class CodeFoldingHighlightingCreator
        {
            private static readonly object ourLock = new object();
            private static Func<string, string, DocumentRange, int, IHighlighting> ourConstructor;

            public static IHighlighting Create(string attributeId, string placeholderText, DocumentRange range,
                int priority)
            {
                if (ourConstructor == null)
                {
                    lock (ourLock)
                    {
                        if (ourConstructor == null)
                            ourConstructor = BuildConstructorLambda();
                    }
                }

                if (ourConstructor != null)
                {
                    return ourConstructor.Invoke(attributeId, placeholderText, range, priority);
                }

#pragma warning disable 618
                if (Shell.Instance.IsTestShell)
                {
                    return new TestableCodeFoldingHighlight(range, attributeId);
                }
#pragma warning restore 618

                return null;
            }

            private static Func<string, string, DocumentRange, int, IHighlighting> BuildConstructorLambda()
            {
                var type = Type.GetType(
                    "JetBrains.ReSharper.Host.Features.Foldings.CodeFoldingHighlighting, JetBrains.ReSharper.Host");
                if (type != null)
                {
                    var constructorInfo = type.GetConstructor(new[]
                        {typeof(string), typeof(string), typeof(DocumentRange), typeof(int)});
                    if (constructorInfo != null)
                    {
                        var attributeIdParameter = Expression.Parameter(typeof(string), "attributeId");
                        var placeholderTextParameter = Expression.Parameter(typeof(string), "placeholderText");
                        var rangeParameter = Expression.Parameter(typeof(DocumentRange), "range");
                        var priorityParameter = Expression.Parameter(typeof(int), "priority");
                        var ctor = Expression.New(constructorInfo, attributeIdParameter, placeholderTextParameter,
                            rangeParameter, priorityParameter);
                        var lambda = Expression.Lambda<Func<string, string, DocumentRange, int, IHighlighting>>(ctor,
                            attributeIdParameter, placeholderTextParameter, rangeParameter, priorityParameter);
                        return lambda.Compile();
                    }
                }

                return null;
            }

            [StaticSeverityHighlighting(Severity.INFO, "CodeFoldingHighlighting", OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false)]
            private class TestableCodeFoldingHighlight : ICustomAttributeIdHighlighting
            {
                private readonly DocumentRange myRange;

                public TestableCodeFoldingHighlight(DocumentRange range, string attributeId)
                {
                    myRange = range;
                    AttributeId = attributeId;
                }

                public bool IsValid() => true;
                public DocumentRange CalculateRange() => myRange;
                public string ToolTip => null;
                public string ErrorStripeToolTip => null;
                public string AttributeId { get; }
            }
        }
    }
}

#endif