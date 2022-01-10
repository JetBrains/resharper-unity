using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public sealed class BurstObjectCreationExpressionAnalyzer : BurstProblemAnalyzerBase<IObjectCreationExpression>, IBurstBannedAnalyzer
    {
        protected override bool CheckAndAnalyze(IObjectCreationExpression objectCreationExpression, IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            if (!IsBurstPermittedType(objectCreationExpression.Type()))
            {
                consumer?.AddHighlighting(new BurstCreatingManagedTypeWarning(objectCreationExpression, (objectCreationExpression.ConstructorReference.Resolve().DeclaredElement as IConstructor)?.GetContainingType()?.ShortName));
                return true;
            }

            return false;
        }
    }
}