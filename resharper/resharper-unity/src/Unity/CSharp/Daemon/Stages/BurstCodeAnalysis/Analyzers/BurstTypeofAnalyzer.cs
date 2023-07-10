using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public sealed class BurstTypeofAnalyzer : BurstProblemAnalyzerBase<ITypeofExpression>, IBurstBannedAnalyzer
    {
        protected override bool CheckAndAnalyze(ITypeofExpression typeofExpression, IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            consumer?.AddHighlighting(new BurstTypeofExpressionWarning(typeofExpression));
            return true;
        }
    }    
    
    [SolutionComponent]
    public sealed class BurstObjectElementAccessAnalyzer : BurstProblemAnalyzerBase<IElementAccessExpression>, IBurstBannedAnalyzer
    {
        protected override bool CheckAndAnalyze(IElementAccessExpression elementAccessExpression, IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            var elementAccessExpressionReference = elementAccessExpression.ElementAccessReference;

            if (elementAccessExpressionReference.Resolve().DeclaredElement is not IProperty invokedAccessor)
                return false;

            if(invokedAccessor.ContainingType is not IClass accessorContainingType)
                return false;
            
            consumer?.AddHighlighting(new BurstAccessingManagedIndexerWarning(elementAccessExpression,
                 accessorContainingType.GetClrName().FullName));
            return true;
        }
    }
}