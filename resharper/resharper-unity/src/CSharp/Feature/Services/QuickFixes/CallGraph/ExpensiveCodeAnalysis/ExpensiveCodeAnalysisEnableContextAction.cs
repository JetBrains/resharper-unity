using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class ExpensiveCodeAnalysisEnableContextAction : ExpensiveCodeAnalysisActionBase
    {
        private const string MESSAGE = "Disable Expensive code analysis";
        private readonly ICSharpContextActionDataProvider myDataProvider;

        public ExpensiveCodeAnalysisEnableContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
            var identifier = dataProvider.GetSelectedTreeNode<ITreeNode>() as ICSharpIdentifier;
            // var selectedTreeNode = dataProvider.GetSelectedElement<ITreeNode>(); CGTD difference?
            MethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
        }

        protected override IMethodDeclaration MethodDeclaration { get; }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.ExpensiveCodeAnalysisEnableAttribute;

        protected override IClrTypeName AntagonistAttribute => null;

        public override string Text => MESSAGE;
        public override IEnumerable<IntentionAction> CreateBulbItems()
        {  
            var solution = myDataProvider.Solution;
            var swea = solution.GetComponent<SolutionAnalysisService>();
            if(swea.Configuration?.Enabled?.Value == false)
                yield break;
            var isGlobalStage = swea.Configuration?.Completed?.Value == true;
            var callGraphSwaExtensionProvider = solution.GetComponent<CallGraphSwaExtensionProvider>();
            var method = MethodDeclaration?.DeclaredElement;
            var elementIdProvider = solution.GetComponent<IElementIdProvider>();
            var methodId = elementIdProvider.GetElementId(method);
            var isExpensiveContext = methodId.HasValue && callGraphSwaExtensionProvider.IsMarkedByCallGraphRootMarksProvider(
                ExpensiveCodeCallGraphAnalyzer.ProviderId, isGlobalStage, methodId.Value);
            if (isExpensiveContext)
                yield return this.ToContextActionIntention();
        }
    }
}