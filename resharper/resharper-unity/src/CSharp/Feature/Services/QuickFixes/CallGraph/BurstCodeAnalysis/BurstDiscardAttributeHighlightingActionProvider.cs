using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
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
    [CustomHighlightingActionProvider(typeof(CSharpProjectFileType))]
    public sealed class BurstDiscardAttributeHighlightingActionProvider : ICustomHighlightingActionProvider
    {
        public IEnumerable<IntentionAction> GetActions(IHighlighting highlighting, DocumentRange range,
            IPsiSourceFile sourceFile,
            IAnchor configureAnchor)
        {
            //CGTD repeat
            if (!(highlighting is IBurstHighlighting))
                yield break;
            
            if (!highlighting.IsValid())
                yield break;

            var psiFile = sourceFile.GetDominantPsiFile<CSharpLanguage>() as ICSharpFile;
            if (psiFile == null) yield break;

            var treeTextRange = psiFile.Translate(range);
            if (!treeTextRange.IsValid()) yield break;

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
                yield break;

            var commentGroup = new SubmenuAnchor(configureAnchor, SubmenuBehavior.ExecutableDuplicateFirst,
                ConfigureHighlightingAnchor.SuppressPosition);
            var commentByAttributesAnchor = new InvisibleAnchor(commentGroup);
            var context = psiFile.FindNodeAt(treeTextRange);
            foreach (var action in CreateActions(context))
            {
                yield return action.ToConfigureActionIntention(commentByAttributesAnchor);
            }
            foreach (var action in CreateActions(context))
            {
                yield return action.ToConfigureActionIntention(configureAnchor);
            }
        }

        [NotNull]
        private static IEnumerable<IBulbAction> CreateActions([CanBeNull] ITreeNode context)
        {
            var methodDeclaration = context?.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration == null)
                yield break;

            //CGTD asserts
            Assertion.Assert(methodDeclaration.DeclaredElement
                    ?.HasAttributeInstance(KnownTypes.BurstDiscardAttribute, AttributesSource.Self) == false,
                "no highlightings allowed at burst discarded method");

            Assertion.Assert(
                methodDeclaration.DeclaredElement?.HasAttributeInstance(
                    CallGraphActionUtil.BurstCodeAnalysisDisableAttribute, AttributesSource.Self) == false,
                "no highlightings allowed at burst disabled method");

            yield return new BurstDiscardAttributeAction(methodDeclaration);
        }
    }
}