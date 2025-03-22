using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage
{
    [DerivedComponentsInstantiationRequirement(InstantiationRequirement.DeadlockSafe)]
    public interface ICallGraphProblemAnalyzer
    {
        void RunInspection([NotNull] ITreeNode node, [NotNull] IHighlightingConsumer consumer, [NotNull] IReadOnlyCallGraphContext context);
    }
}