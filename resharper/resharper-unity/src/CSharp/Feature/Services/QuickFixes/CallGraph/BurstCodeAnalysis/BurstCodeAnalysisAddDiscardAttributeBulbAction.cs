using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public sealed class BurstCodeAnalysisAddDiscardAttributeBulbAction : BurstCodeAnalysisAddDiscardAttributeActionBase
    {
        public BurstCodeAnalysisAddDiscardAttributeBulbAction(IMethodDeclaration methodDeclaration)
        {
            MethodDeclaration = methodDeclaration;
        }

        protected override IMethodDeclaration MethodDeclaration { get; }
        public override IEnumerable<IntentionAction> CreateBulbItems() => throw new System.NotImplementedException();
    }
}