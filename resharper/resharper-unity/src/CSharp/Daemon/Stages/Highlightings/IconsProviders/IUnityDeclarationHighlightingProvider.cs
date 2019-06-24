using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders
{
    public interface IUnityDeclarationHighlightingProvider
    {
        // Return declared element of marked node
        IDeclaredElement Analyze(IDeclaration treeNode, IHighlightingConsumer consumer, DaemonProcessKind kind);
    }
}