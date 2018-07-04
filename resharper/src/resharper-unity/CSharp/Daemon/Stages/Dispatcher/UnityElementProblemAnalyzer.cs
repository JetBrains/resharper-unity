using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher
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
            var processKind = data.GetDaemonProcessKind();
            if (processKind != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (!element.GetProject().IsUnityProject())
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);
    }
}