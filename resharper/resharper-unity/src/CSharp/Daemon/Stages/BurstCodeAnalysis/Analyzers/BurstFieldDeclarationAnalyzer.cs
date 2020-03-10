using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    // [SolutionComponent]
    // CGTD currently field declarations not supported due to lack of context functionality
    public class BurstFieldDeclarationAnalyzer : BurstProblemAnalyzerBase<ITypeMemberDeclaration>
    {
        protected override void Analyze(ITypeMemberDeclaration typeMemberDeclaration, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            if (typeMemberDeclaration is ITypeOwnerDeclaration typeOwnerDeclaration && typeOwnerDeclaration.Type.IsReferenceType())
            {
                consumer.AddHighlighting(new BurstWarning(typeMemberDeclaration.GetDocumentRange(), "Job structs may not contain any reference types"));
            }
        }
    }
}