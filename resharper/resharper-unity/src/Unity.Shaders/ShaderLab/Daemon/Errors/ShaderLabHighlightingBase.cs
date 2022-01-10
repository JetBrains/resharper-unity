namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Errors
{
    public abstract class ShaderLabHighlightingBase
    {
        // ErrorsGen makes IsValid override if we specify a base class
        public abstract bool IsValid();
    }
}