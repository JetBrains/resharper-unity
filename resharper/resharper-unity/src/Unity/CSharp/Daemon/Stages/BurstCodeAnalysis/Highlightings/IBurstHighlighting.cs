using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings
{
    public interface IBurstHighlighting : IHighlighting
    {
        ITreeNode Node { get; }
    }
}