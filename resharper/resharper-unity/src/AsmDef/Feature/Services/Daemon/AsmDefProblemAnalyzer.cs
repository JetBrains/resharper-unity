using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    public abstract class AsmDefProblemAnalyzer<T> : ElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            var processKind = data.GetDaemonProcessKind();
            if (processKind != DaemonProcessKind.VISIBLE_DOCUMENT && processKind != DaemonProcessKind.SOLUTION_ANALYSIS)
                return;

            if (data.SourceFile == null || !element.Language.Is<JsonNewLanguage>() || !data.SourceFile.IsAsmDef())
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);
    }
}
