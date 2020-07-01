using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstObjectCreationExpressionAnalyzer : BurstProblemAnalyzerBase<IObjectCreationExpression>
    {
        protected override bool CheckAndAnalyze(IObjectCreationExpression objectCreationExpression, IHighlightingConsumer consumer)
        {
            if (!IsBurstPermittedType(objectCreationExpression.Type()))
            {
                consumer?.AddHighlighting(new BC1021Error(objectCreationExpression.GetDocumentRange(), (objectCreationExpression.ConstructorReference.Resolve().DeclaredElement as IConstructor)?.GetContainingType()?.ShortName));
                return true;
            }

            return false;
        }
    }
}