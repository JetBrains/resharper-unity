using System;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Json.Psi.Resolve;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Util;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Json.Daemon.ContextHighlighters
{
    [ContainsContextConsumer]
    public class AsmDefUsagesContextHighlighter : ContextHighlighterBase
    {
        [NotNull] private readonly IDeclaredElement myDeclaredElement;
        [CanBeNull] private readonly IJavaScriptLiteralExpression myLiteralExpressionUnderCaret;

        private AsmDefUsagesContextHighlighter(IDeclaredElement declaredElement,
            [CanBeNull] IJavaScriptLiteralExpression literalExpressionUnderCaret)
        {
            myDeclaredElement = declaredElement;
            myLiteralExpressionUnderCaret = literalExpressionUnderCaret;
        }

        private const string HIGHLIGHTING_ID = HighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessContext(
            [NotNull] Lifetime lifetime,
            [NotNull] HighlightingProlongedLifetime prolongedLifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView
        )
        {
            var psiView = psiDocumentRangeView.View<JsonLanguage>();
            foreach (var file in psiView.SortedSourceFiles)
            {
                if (!file.IsAsmDef()) return null;
            }

            var declaredElement = FindDeclaredElement(psiView, out var literalExpressionUnderCaret);
            if (declaredElement == null) return null;

            var highlighter = new AsmDefUsagesContextHighlighter(declaredElement, literalExpressionUnderCaret);
            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }

        private static IDeclaredElement FindDeclaredElement(IPsiView psiView,
            out IJavaScriptLiteralExpression literalExpressionUnderCaret)
        {
            literalExpressionUnderCaret = null;

            var expression = psiView.GetSelectedTreeNode<IJavaScriptLiteralExpression>();
            if (expression.IsNameStringLiteralValue())
            {
                literalExpressionUnderCaret = expression;

                // TODO: Not sure I like creating a new DeclaredElement here
                return new AsmDefNameDeclaredElement(expression.GetJavaScriptServices(), expression.GetUnquotedText(),
                    expression.GetSourceFile(), expression.GetTreeStartOffset().Offset);
            }

            if (expression.IsReferencesStringLiteralValue())
            {
                var reference = expression.FindReference<AsmDefNameReference>();
                if (reference != null)
                    return reference.Resolve().DeclaredElement;
            }

            return null;
        }

        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            if (myLiteralExpressionUnderCaret != null)
                HighlightFoundDeclaration(myLiteralExpressionUnderCaret, consumer);
            else
            {
                var psiView = psiDocumentRangeView.View<JsonLanguage>(PsiLanguageCategories.Dominant);
                HighlightDeclarationsInFile(myDeclaredElement, psiView, consumer);
            }

            HighlightReferencesInFile(myDeclaredElement, psiDocumentRangeView, consumer);
        }

        private static void HighlightFoundDeclaration(IJavaScriptLiteralExpression literalExpression, HighlightingsConsumer consumer)
        {
            var range = literalExpression.GetUnquotedDocumentRange();
            if (range.IsValid())
                consumer.ConsumeHighlighting(HIGHLIGHTING_ID, range);
        }

        private void HighlightDeclarationsInFile(IDeclaredElement declaredElement, IPsiView psiView, HighlightingsConsumer consumer)
        {
            // There are no IDeclarations for this declared element, try and find the associated string literal expression
            var asmdefNameDeclaredElement = declaredElement as AsmDefNameDeclaredElement;
            if (asmdefNameDeclaredElement == null)
                return;

            foreach (var psiSourceFile in psiView.SortedSourceFiles)
            {
                if (psiSourceFile != asmdefNameDeclaredElement.SourceFile)
                    continue;

                var primaryPsiFile = psiSourceFile.GetPrimaryPsiFile();
                var node = primaryPsiFile?.FindNodeAt(TreeTextRange.FromLength(
                    new TreeOffset(asmdefNameDeclaredElement.DeclarationOffset),
                    asmdefNameDeclaredElement.ShortName.Length));
                var literalExpression = node?.GetContainingNode<IJavaScriptLiteralExpression>();
                if (literalExpression != null)
                    HighlightFoundDeclaration(literalExpression, consumer);
            }
        }

        private static void HighlightReferencesInFile(IDeclaredElement declaredElement, IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(psiDocumentRangeView.SortedSourceFiles);
            var elements = new[] {new DeclaredElementInstance(declaredElement)};
            var searchRequest = new SearchSingleFileDeclaredElementRequest(elements, elements, searchDomain);

            foreach (var occurrence in searchRequest.Search(NullProgressIndicator.Create()) ?? EmptyList<IOccurrence>.InstanceList)
            {
                if (!(occurrence is ReferenceOccurrence referenceOccurrence)) continue;

                var primaryReference = referenceOccurrence.PrimaryReference;
                if (primaryReference == null) continue;

                var documentRange = primaryReference.GetDocumentRange();
                consumer.ConsumeHighlighting(HIGHLIGHTING_ID, documentRange);
            }
        }
    }
}