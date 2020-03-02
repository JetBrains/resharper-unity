using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstIParametersOwnerDeclaration : BurstProblemAnalyzerBase<IParametersOwnerDeclaration>
    {
        protected override void Analyze(IParametersOwnerDeclaration parametersOwnerDeclaration, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            foreach (var parameterDeclaration in parametersOwnerDeclaration.ParameterDeclarations)
            {
                if (parameterDeclaration is ICSharpParameterDeclaration cSharpParameterDeclaration)
                {
                    //CGTD does interfaces allowed as parameters?
                    if (!cSharpParameterDeclaration.Type.IsSuitableForBurst())
                    {
                        consumer.AddHighlighting(new BurstWarning(cSharpParameterDeclaration.TypeUsage.GetDocumentRange(),
                            "using managed object as parameter"));
                    }
                }
            }
            
            if (!(parametersOwnerDeclaration.DeclaredParametersOwner?.ReturnType.IsSuitableForBurst() ?? false))
            {
                switch (parametersOwnerDeclaration)
                {
                    case IIndexerDeclaration indexerDeclaration:
                    {
                        consumer.AddHighlighting(new BurstWarning(indexerDeclaration.TypeUsage.GetDocumentRange(), "returning managed object"));
                        break;
                    }
                    case ILocalFunctionDeclaration localFunctionDeclaration:
                    {
                        consumer.AddHighlighting(new BurstWarning(localFunctionDeclaration.TypeUsage.GetDocumentRange(), "returning managed object"));
                        break;
                    }
                    case IMethodDeclaration methodDeclaration:
                    {
                        consumer.AddHighlighting(new BurstWarning(methodDeclaration.TypeUsage.GetDocumentRange(), "returning managed object"));
                        break;
                    }
                }
            }
        }
    }
}