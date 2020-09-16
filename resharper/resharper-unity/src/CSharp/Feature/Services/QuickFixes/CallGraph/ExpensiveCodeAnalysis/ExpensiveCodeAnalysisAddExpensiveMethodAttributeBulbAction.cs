using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public class ExpensiveCodeAnalysisAddExpensiveMethodAttributeBulbAction : ExpensiveCodeAnalysisAddExpensiveMethodAttributeActionBase
    {
        public ExpensiveCodeAnalysisAddExpensiveMethodAttributeBulbAction(IMethodDeclaration methodDeclaration)
        {
            MethodDeclaration = methodDeclaration;
        }

        protected override IMethodDeclaration MethodDeclaration { get; }
        public override IEnumerable<IntentionAction> CreateBulbItems() => throw new System.NotImplementedException();
    }
}