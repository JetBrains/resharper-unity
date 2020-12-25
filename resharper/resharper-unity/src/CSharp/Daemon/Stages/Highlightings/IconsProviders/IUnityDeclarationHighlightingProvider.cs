using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public interface IUnityDeclarationHighlightingProvider
    {
        /// <summary>
        /// Process declarations and add specific unity highlightings
        /// </summary>
        /// <returns>true if highlighting to declaration was added, otherwise false</returns>
        bool AddDeclarationHighlighting(IDeclaration treeNode, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context);
    }
}