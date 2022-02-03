using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon
{
    public abstract class ShaderLabElementProblemAnalyzer<T> : ElementProblemAnalyzer<T>
    {
        protected sealed override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var processKind = data.GetDaemonProcessKind();
            if (processKind != DaemonProcessKind.VISIBLE_DOCUMENT && processKind != DaemonProcessKind.SOLUTION_ANALYSIS)
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);
    }
}