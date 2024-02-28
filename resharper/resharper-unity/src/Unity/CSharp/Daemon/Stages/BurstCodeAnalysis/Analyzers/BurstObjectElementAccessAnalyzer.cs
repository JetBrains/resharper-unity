using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class BurstObjectElementAccessAnalyzer : BurstProblemAnalyzerBase<IElementAccessExpression>, IBurstBannedAnalyzer
    {
        protected override bool CheckAndAnalyze(IElementAccessExpression elementAccessExpression, IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            var elementAccessExpressionReference = elementAccessExpression.ElementAccessReference;

            if (elementAccessExpressionReference.Resolve().DeclaredElement is not IProperty invokedAccessor)
                return false;

            var containingType = invokedAccessor.ContainingType;
            
            if(containingType is not IClass accessorContainingType)
                return false;
            
            if(containingType.GetClrName().Equals(PredefinedType.ARRAY_FQN))
                return false;
            
            consumer?.AddHighlighting(new BurstAccessingManagedIndexerWarning(elementAccessExpression,
                accessorContainingType.GetClrName().FullName));
            return true;
        }
    }
}