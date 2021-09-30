using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon
{
    public abstract class AsmDefProblemAnalyzer<T> : ElementProblemAnalyzer<T>
        where T : ITreeNode
    {
        protected override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            // Run for visible documents and SWEA. Also run for "other", which is used by scoped quick fixes
            if (data.GetDaemonProcessKind() == DaemonProcessKind.GLOBAL_WARNINGS)
                return;

            if (data.SourceFile == null || !element.Language.Is<JsonNewLanguage>() || !data.SourceFile.IsAsmDef())
                return;

            if (!element.GetProject().IsUnityProject())
                return;

            Analyze(element, data, consumer);
        }

        protected abstract void Analyze(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer);
    }
}
