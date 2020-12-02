using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class SimpleMethodSingleContextActionBase : SimpleMethodContextActionBase
    {
        protected SimpleMethodSingleContextActionBase(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IEnumerable<IntentionAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            return GetBulbAction(methodDeclaration).ToContextActionIntentions();
        }

        protected abstract IBulbAction GetBulbAction([NotNull] IMethodDeclaration methodDeclaration);
    }
}