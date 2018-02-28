namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    public abstract class CSharpUnityHighlightingBase : IUnityHighlighting
    {
        // ErrorsGen makes IsValid override if we specify a base class
        public abstract bool IsValid();
    }
}