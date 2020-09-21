using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public sealed class AddDiscardAttributeBulbAction : AddDiscardAttributeActionBase
    {
        public AddDiscardAttributeBulbAction(IMethodDeclaration methodDeclaration)
        {
            MethodDeclaration = methodDeclaration;
        }

        protected override IMethodDeclaration MethodDeclaration { get; }
        public override IEnumerable<IntentionAction> CreateBulbItems() => EmptyList<IntentionAction>.Enumerable;
    }
}