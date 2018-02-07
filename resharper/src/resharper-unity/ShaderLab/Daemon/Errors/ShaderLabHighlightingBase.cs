using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors
{
    public abstract class ShaderLabHighlightingBase : IUnityHighlighting
    {
        public abstract bool IsValid();
    }
}