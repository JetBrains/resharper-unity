using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors
{
    public abstract class ShaderLabHighlightingBase : IUnityHighlighting
    {
        // ErrorsGen makes IsValid override if we specify a base class
        public abstract bool IsValid();
    }
}