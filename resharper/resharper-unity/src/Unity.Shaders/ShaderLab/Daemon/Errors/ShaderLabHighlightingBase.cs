namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors
{
    public abstract class ShaderLabHighlightingBase
    {
        // ErrorsGen makes IsValid override if we specify a base class
        public abstract bool IsValid();
    }
}