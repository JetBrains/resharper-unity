using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Errors
{
    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.ShaderLabErrors_Text), true)]
    public class ShaderLabErrors
    {
    }

    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.ShaderLabWarnings_Text), true)]
    public class ShaderLabWarnings
    {
    }
}