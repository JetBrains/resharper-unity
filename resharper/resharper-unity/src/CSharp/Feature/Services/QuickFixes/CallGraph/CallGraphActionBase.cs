using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph
{
    public abstract class CallGraphActionBase : IBulbAction, IContextAction, IQuickFix
    {
        [CanBeNull] protected readonly IMethodDeclaration MethodDeclaration;

        [NotNull] protected abstract IClrTypeName ProtagonistAttribute { get; }

        [CanBeNull] protected abstract IClrTypeName AntagonistAttribute { get; }

        protected CallGraphActionBase(ICSharpContextActionDataProvider dataProvider)
        {
            var identifier = dataProvider.GetSelectedTreeNode<ITreeNode>() as ICSharpIdentifier;
            // var selectedTreeNode = dataProvider.GetSelectedElement<ITreeNode>(); CGTD difference?
            MethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
        }
        
        protected CallGraphActionBase(IBurstHighlighting burstHighlighting) 
        {
            //CGTD repeat
            if (burstHighlighting == null)
                return;
            
            if (!burstHighlighting.IsValid())
                return;
            
            var psiFile = burstHighlighting.Node.GetSourceFile()?.GetDominantPsiFile<CSharpLanguage>() as ICSharpFile;
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
        
        protected CallGraphActionBase(IMethodDeclaration methodDeclaration)
        {
            MethodDeclaration = methodDeclaration;
        }

        public void Execute(ISolution solution, ITextControl textControl)
        {
            if (MethodDeclaration == null)
                return;

            CallGraphActionUtil.AppendAttributeInTransaction(
                MethodDeclaration,
                ProtagonistAttribute,
                AntagonistAttribute,
                GetType().Name);
        }

        public abstract string Text { get; }

        public IEnumerable<IntentionAction> CreateBulbItems() => this.ToContextActionIntentions();

        public abstract bool IsAvailable(IUserDataHolder cache);
    }
}