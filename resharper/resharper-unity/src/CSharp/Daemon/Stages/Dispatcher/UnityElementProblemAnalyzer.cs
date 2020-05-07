using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher
{
    public abstract class UnityElementProblemAnalyzer<T> : ElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected UnityElementProblemAnalyzer([NotNull] UnityApi unityApi)
        {
            Api = unityApi;
        }

        protected UnityApi Api { get; }

        protected sealed override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            // Run for all daemon kinds except global analysis. Visible document and solution wide analysis are obvious
            // and required. "Other" is used by scoped quick fixes, and incremental solution analysis is only for stages
            // that respond to settings changes. We don't strictly need this, but it won't cause problems.
            var processKind = data.GetDaemonProcessKind();
            if (processKind == DaemonProcessKind.GLOBAL_WARNINGS)
                return;

            if (!element.GetProject().IsUnityProject())
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);
    }
}