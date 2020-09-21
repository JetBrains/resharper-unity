using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.ExpensiveCodeActionsUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public class AddExpensiveMethodAttributeBulbAction : AddExpensiveMethodAttributeActionBase
    {
        public AddExpensiveMethodAttributeBulbAction(IMethodDeclaration methodDeclaration)
        {
            FixedArguments = GetExpensiveAttributeValues(methodDeclaration);
            MethodDeclaration = methodDeclaration;
        }

        protected override IMethodDeclaration MethodDeclaration { get; }
        protected override CompactList<AttributeValue> FixedArguments { get; }
        public override IEnumerable<IntentionAction> CreateBulbItems() => EmptyList<IntentionAction>.Enumerable;
    }
}