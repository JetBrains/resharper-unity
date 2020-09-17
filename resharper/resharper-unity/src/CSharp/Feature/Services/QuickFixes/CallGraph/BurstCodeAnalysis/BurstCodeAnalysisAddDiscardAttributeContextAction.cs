using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = Message,
        Description = Message,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class BurstCodeAnalysisAddDiscardAttributeContextAction : BurstCodeAnalysisAddDiscardAttributeActionBase
    {
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public BurstCodeAnalysisAddDiscardAttributeContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
            var identifier = myDataProvider.GetSelectedTreeNode<ITreeNode>() as ICSharpIdentifier;
            // var selectedTreeNode = dataProvider.GetSelectedElement<ITreeNode>(); CGTD difference?
            MethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
        }

        protected override IMethodDeclaration MethodDeclaration { get; }

        public override IEnumerable<IntentionAction> CreateBulbItems()
        {
            var method = MethodDeclaration?.DeclaredElement;
            // burst discard - always no if it is already discarded.
            if(BurstCodeAnalysisUtil.IsBurstContextBannedFunction(method))
                yield break;
            var solution = myDataProvider.Solution;
            var swea = solution.GetComponent<SolutionAnalysisService>();
            if(swea.Configuration?.Enabled?.Value == false)
                yield break;
            var isGlobalStage = swea.Configuration?.Completed?.Value == true;
            var callGraphSwaExtensionProvider = solution.GetComponent<CallGraphSwaExtensionProvider>();
            var elementIdProvider = solution.GetComponent<IElementIdProvider>();
            var methodId = elementIdProvider.GetElementId(method);
            var isBurstContext = methodId.HasValue && callGraphSwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(
                BurstMarksProvider.ProviderId, isGlobalStage, methodId.Value);
            if (isBurstContext)
                yield return this.ToContextActionIntention();
        }
    }
}