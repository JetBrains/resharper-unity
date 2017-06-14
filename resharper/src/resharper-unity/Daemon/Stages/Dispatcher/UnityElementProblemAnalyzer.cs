using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Dispatcher
{
    public abstract class UnityElementProblemAnalyzer<T> : ElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected UnityElementProblemAnalyzer(UnityApi unityApi)
        {
            Api = unityApi;
        }

        protected UnityApi Api { get; }

        protected sealed override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            if (data.ProcessKind != DaemonProcessKind.VISIBLE_DOCUMENT)
                return;

            if (!element.GetProject().IsUnityProject())
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);

        protected void AddGutterMark(T element, DocumentRange documentRange, string tooltip, IHighlightingConsumer consumer)
        {
            var highlighting = new UnityMarkOnGutter(Api, element, documentRange, tooltip);
            consumer.AddHighlighting(highlighting, documentRange);
        }
    }
}