using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature
{
    public abstract class AsmDefUsagesContextHighlighterBase<TLiteralNode, TLanguage, TDeclaredElement> : ContextHighlighterBase 
        where TLiteralNode : class, ITreeNode 
        where TLanguage : PsiLanguageType
        where TDeclaredElement : class, IAsmDefDeclaredElement
    {
        [NotNull] private readonly IDeclaredElement myDeclaredElement;
        [CanBeNull] private readonly TLiteralNode myLiteralExpressionUnderCaret;

        protected AsmDefUsagesContextHighlighterBase(IDeclaredElement declaredElement,
            [CanBeNull] TLiteralNode literalExpressionUnderCaret)
        {
            myDeclaredElement = declaredElement;
            myLiteralExpressionUnderCaret = literalExpressionUnderCaret;
        }

        private const string HIGHLIGHTING_ID = HighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

        protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
        {
            if (myLiteralExpressionUnderCaret != null)
                HighlightFoundDeclaration(myLiteralExpressionUnderCaret, consumer);
            else
            {
                var psiView = psiDocumentRangeView.View<TLanguage>(PsiLanguageCategories.Dominant);
                HighlightDeclarationsInFile(myDeclaredElement, psiView, consumer);
            }

            HighlightReferencesInFile(myDeclaredElement, psiDocumentRangeView, consumer);
        }

        protected abstract void HighlightFoundDeclaration(TLiteralNode node, HighlightingsConsumer consumer);
        
        protected void HighlightDeclarationsInFile(IDeclaredElement declaredElement, IPsiView psiView, HighlightingsConsumer consumer)
        {
            // There are no IDeclarations for this declared element, try and find the associated string literal expression
            var asmdefNameDeclaredElement = declaredElement as TDeclaredElement;
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
                var literalExpression = node?.GetContainingNode<TLiteralNode>();
                if (literalExpression != null)
                    HighlightFoundDeclaration(literalExpression, consumer);
            }
        }

        protected static void HighlightReferencesInFile(IDeclaredElement declaredElement, IPsiDocumentRangeView psiDocumentRangeView, HighlightingsConsumer consumer)
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