using System.Collections.Generic;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [QuickFix]
    public sealed class BurstCodeAnalysisAddDiscardAttributeQuickFix : BurstCodeAnalysisAddDiscardAttributeActionBase
    {
        public BurstCodeAnalysisAddDiscardAttributeQuickFix(IBurstHighlighting burstHighlighting)
        {
            //CGTD copy from DisableBySuppress
            if (burstHighlighting == null)
                return;

            if (!burstHighlighting.IsValid())
                return;

            var psiFile = burstHighlighting?.Node?.GetSourceFile()?.GetDominantPsiFile<CSharpLanguage>() as ICSharpFile;
            if (psiFile == null) return;

            var treeTextRange = psiFile.Translate(burstHighlighting.Node.GetDocumentRange());
            if (!treeTextRange.IsValid()) return;

            var nodeToCheck = psiFile.FindNodeAt(treeTextRange);
            var scopeToCheck = nodeToCheck?.GetContainingNode<IScope>(true);
            DocumentRange documentRangeToCheck;
            switch (scopeToCheck)
            {
                case IStatementsOwner statementsOwner:
                    documentRangeToCheck = CSharpModificationUtil
                        .GetHolderBlockRanges(statementsOwner)
                        .Where(treeRange => Enumerable.Contains(treeRange, scopeToCheck))
                        .DefaultIfEmpty(new TreeRange(scopeToCheck))
                        .First().GetDocumentRange();
                    break;

                case IScope scope:
                    documentRangeToCheck = scope.GetDocumentRange();
                    break;

                default:
                    documentRangeToCheck = nodeToCheck.GetDocumentRange();
                    break;
            }

            if (!documentRangeToCheck.IsValid())
                return;

            var context = psiFile.FindNodeAt(treeTextRange);
            MethodDeclaration = context?.GetContainingNode<IMethodDeclaration>();
        }

        protected override IMethodDeclaration MethodDeclaration { get; }
        public override IEnumerable<IntentionAction> CreateBulbItems() => this.ToContextActionIntentions(IntentionsAnchors.ContextActionsAnchor, BulbThemedIcons.YellowBulb.Id);
    }
}