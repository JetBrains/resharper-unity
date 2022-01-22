using System;
using JetBrains.Application.Progress;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.ContextHighlighters
{
    [ContainsContextConsumer]
    public class ShaderLabUsagesContextHighlighter : ContextHighlighterBase
    {
        private readonly IDeclaredElement myDeclaredElement;
        private readonly IDeclaration? myDeclarationUnderCaret;

        private ShaderLabUsagesContextHighlighter(IDeclaredElement declaredElement, IDeclaration? declarationUnderCaret)
        {
            myDeclaredElement = declaredElement;
            myDeclarationUnderCaret = declarationUnderCaret;
        }

        private const string HIGHLIGHTING_ID = GeneralHighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

        [AsyncContextConsumer]
        public static Action? ProcessContext(
            Lifetime lifetime, HighlightingProlongedLifetime prolongedLifetime,
            [ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView,
            ShaderLabUsageContextHighlighterAvailability contextHighlighterAvailability)
        {
            var psiView = psiDocumentRangeView.View<ShaderLabLanguage>();

            foreach (var file in psiView.SortedSourceFiles)
            {
                if (!contextHighlighterAvailability.IsAvailable(file)) return null;
            }

            var declaredElement = FindDeclaredElement(psiView, out var declarationUnderCaret);
            if (declaredElement == null) return null;

            var highlighter = new ShaderLabUsagesContextHighlighter(declaredElement, declarationUnderCaret);
            return highlighter.GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
        }

        private static IDeclaredElement? FindDeclaredElement(IPsiView psiView, out IDeclaration? declarationUnderCaret)
        {
            declarationUnderCaret = null;

            var referenceName = psiView.GetSelectedTreeNode<IReferenceName>();
            if (referenceName != null)
                return referenceName.Reference.Resolve().DeclaredElement;

            var identifier = psiView.GetSelectedTreeNode<IShaderLabIdentifier>();
            declarationUnderCaret = PropertyDeclarationNavigator.GetByName(identifier);
            return declarationUnderCaret?.DeclaredElement;
        }

        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            if (myDeclarationUnderCaret != null)
                HighlightDeclaration(myDeclarationUnderCaret, consumer);
            else
            {
                var psiView = psiDocumentRangeView.View<ShaderLabLanguage>(PsiLanguageCategories.Dominant);
                HighlightDeclarationsInFile(myDeclaredElement, psiView, consumer);
            }
            HighlightReferencesInFile(myDeclaredElement, psiDocumentRangeView, consumer);
        }

        private void HighlightReferencesInFile(IDeclaredElement declaredElement,
            IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(psiDocumentRangeView.SortedSourceFiles);
            var elements = new[] {new DeclaredElementInstance(declaredElement)};
            var searchRequest = new SearchSingleFileDeclaredElementRequest(elements, elements, searchDomain);

            // Search is [CanBeNull] on base type, but derived types always return a value
            foreach (var occurrence in searchRequest.Search(NullProgressIndicator.Create())!)
            {
                if (!(occurrence is ReferenceOccurrence referenceOccurrence)) continue;

                var primaryReference = referenceOccurrence.PrimaryReference;
                if (primaryReference == null) continue;

                var documentRange = primaryReference.GetDocumentRange();
                consumer.ConsumeHighlighting(HIGHLIGHTING_ID, documentRange);
            }
        }

        private void HighlightDeclaration(IDeclaration declaration, HighlightingsConsumer consumer)
        {
            var nameDocumentRange = declaration.GetNameDocumentRange();
            if (nameDocumentRange.IsValid())
                consumer.ConsumeHighlighting(HIGHLIGHTING_ID, nameDocumentRange);
        }

        private void HighlightDeclarationsInFile(IDeclaredElement declaredElement, IPsiView psiView,
            HighlightingsConsumer consumer)
        {
            foreach (var psiSourceFile in psiView.SortedSourceFiles)
            foreach (var declaration in declaredElement.GetDeclarationsIn(psiSourceFile))
                HighlightDeclaration(declaration, consumer);
        }
    }
}
