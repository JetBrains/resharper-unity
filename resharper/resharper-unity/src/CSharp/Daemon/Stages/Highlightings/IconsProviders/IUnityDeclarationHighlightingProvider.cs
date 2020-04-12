using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public interface IUnityDeclarationHighlightingProvider
    {
        /// <summary>
        /// Process declarations and aadd specific unity highlightings
        /// </summary>
        /// <returns>true if highlighting to declaration was added, otherwise false</returns>
        bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer, DaemonProcessKind kind);
    }
}