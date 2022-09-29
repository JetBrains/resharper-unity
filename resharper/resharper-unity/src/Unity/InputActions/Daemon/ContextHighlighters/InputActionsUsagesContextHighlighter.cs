#nullable enable

using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.InputActions.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.InputActions.Daemon.ContextHighlighters
{
    [ContainsContextConsumer]
    public class InputActionsUsagesContextHighlighter : ContextHighlighterBase
    {
        private const string HIGHLIGHTING_ID = GeneralHighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

        private readonly IDeclaredElement myDeclaredElement;
        private readonly IJsonNewLiteralExpression? myLiteralExpressionUnderCaret;

        private InputActionsUsagesContextHighlighter(IDeclaredElement declaredElement,
                                               IJsonNewLiteralExpression? literalExpressionUnderCaret)
        {
            myDeclaredElement = declaredElement;
            myLiteralExpressionUnderCaret = literalExpressionUnderCaret;
        }

        [AsyncContextConsumer]
        public static Action? ProcessContext(
            Lifetime lifetime,
            HighlightingProlongedLifetime prolongedLifetime,
            [ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView
        )
        {
            var isInputActions = false;

            var psiView = psiDocumentRangeView.View<JsonNewLanguage>();
            foreach (var file in psiView.SortedSourceFiles)
            {
                isInputActions |= file.IsInputActions();
            }

            if (!isInputActions) return null;

            IJsonNewLiteralExpression? literalExpressionUnderCaret = null;
            var declaredElement = isInputActions
                ? FindDeclaredElement(psiView.SortedSourceFiles.First(), psiView, out literalExpressionUnderCaret) //todo: revisit here
                : null;
            if (declaredElement == null) return null;

            var highlighter = new InputActionsUsagesContextHighlighter(declaredElement, literalExpressionUnderCaret);
            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }

        private static IDeclaredElement? FindDeclaredElement(IPsiSourceFile file, IPsiView psiView,
                                                                      out IJsonNewLiteralExpression? literalExpressionUnderCaret)
        {
            var cache = file.GetSolution().GetComponent<InputActionsCache>();
            literalExpressionUnderCaret = null;

            var expression = psiView.GetSelectedTreeNode<IJsonNewLiteralExpression>();
            if (expression == null)
                return null;

            if (cache.ContainsOffset(file, expression))
            {
                literalExpressionUnderCaret = expression;
            
                return new InputActionsDeclaredElement(expression.GetUnquotedText(), expression.GetSourceFile(),
                    expression.GetTreeStartOffset().Offset);
            }

            return null;
        }

        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView,
                                                     HighlightingsConsumer consumer)
        {
            if (myLiteralExpressionUnderCaret != null)
                HighlightFoundDeclaration(myLiteralExpressionUnderCaret, consumer);
            else
            {
                var psiView = psiDocumentRangeView.View<JsonNewLanguage>(PsiLanguageCategories.Dominant);
                HighlightDeclarationsInFile(myDeclaredElement, psiView, consumer);
            }

            HighlightReferencesInFile(myDeclaredElement, psiDocumentRangeView, consumer);
        }

        private void HighlightFoundDeclaration(IJsonNewLiteralExpression literalExpression, HighlightingsConsumer consumer)
        {
            var range = literalExpression.GetUnquotedDocumentRange();
            if (range.IsValid())
                consumer.ConsumeHighlighting(HIGHLIGHTING_ID, range);
        }

        private void HighlightDeclarationsInFile(IDeclaredElement declaredElement, IPsiView psiView, HighlightingsConsumer consumer)
        {
            // There are no IDeclarations for this declared element, try and find the associated string literal expression
            var inputActionsDeclaredElement = declaredElement as InputActionsDeclaredElement;
            if (inputActionsDeclaredElement == null)
                return;

            foreach (var psiSourceFile in psiView.SortedSourceFiles)
            {
                if (psiSourceFile != inputActionsDeclaredElement.SourceFile)
                    continue;

                var primaryPsiFile = psiSourceFile.GetPrimaryPsiFile();
                var node = primaryPsiFile?.FindNodeAt(TreeTextRange.FromLength(
                    new TreeOffset(inputActionsDeclaredElement.DeclarationOffset),
                    inputActionsDeclaredElement.ShortName.Length));
                var literalExpression = node?.GetContainingNode<IJsonNewLiteralExpression>();
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