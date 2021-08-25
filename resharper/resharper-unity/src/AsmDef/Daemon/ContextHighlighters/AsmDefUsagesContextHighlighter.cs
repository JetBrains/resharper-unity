using System;
using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.ContextHighlighters
{
    [ContainsContextConsumer]
    public class AsmDefUsagesContextHighlighter : AsmDefUsagesContextHighlighterBase<IJsonNewLiteralExpression, JsonNewLanguage, AsmDefNameDeclaredElement>
    {
        [NotNull] private readonly IDeclaredElement myDeclaredElement;
        [CanBeNull] private readonly IJsonNewLiteralExpression myLiteralExpressionUnderCaret;

        private AsmDefUsagesContextHighlighter(IDeclaredElement declaredElement,
            [CanBeNull] IJsonNewLiteralExpression literalExpressionUnderCaret) : base(declaredElement, literalExpressionUnderCaret)
        {
            myDeclaredElement = declaredElement;
            myLiteralExpressionUnderCaret = literalExpressionUnderCaret;
        }

        private const string HIGHLIGHTING_ID = HighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

        [CanBeNull, AsyncContextConsumer]
        public static Action ProcessContext(
            Lifetime lifetime,
            [NotNull] HighlightingProlongedLifetime prolongedLifetime,
            [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))] IPsiDocumentRangeView psiDocumentRangeView
        )
        {
            var psiView = psiDocumentRangeView.View<JsonNewLanguage>();
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
            out IJsonNewLiteralExpression literalExpressionUnderCaret)
        {
            literalExpressionUnderCaret = null;

            var expression = psiView.GetSelectedTreeNode<IJsonNewLiteralExpression>();
            if (expression.IsNameLiteral())
            {
                literalExpressionUnderCaret = expression;

                // TODO: Not sure I like creating a new DeclaredElement here
                return new AsmDefNameDeclaredElement(expression.GetUnquotedText(), expression.GetSourceFile(), expression.GetTreeStartOffset().Offset);
            }

            if (expression.IsReferenceLiteral())
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
                var psiView = psiDocumentRangeView.View<JsonNewLanguage>(PsiLanguageCategories.Dominant);
                HighlightDeclarationsInFile(myDeclaredElement, psiView, consumer);
            }

            HighlightReferencesInFile(myDeclaredElement, psiDocumentRangeView, consumer);
        }

        protected override void HighlightFoundDeclaration(IJsonNewLiteralExpression literalExpression, HighlightingsConsumer consumer)
        {
            var range = literalExpression.GetUnquotedDocumentRange();
            if (range.IsValid())
                consumer.ConsumeHighlighting(HIGHLIGHTING_ID, range);
        }
    }
}