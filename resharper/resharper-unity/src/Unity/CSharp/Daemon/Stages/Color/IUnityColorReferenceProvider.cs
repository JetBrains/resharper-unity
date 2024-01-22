#nullable enable
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Color;

public interface IUnityColorReferenceProvider
{
    IColorReference? GetColorReference(ITreeNode element);
}