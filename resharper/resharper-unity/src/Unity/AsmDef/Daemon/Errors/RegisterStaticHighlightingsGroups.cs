using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors
{
    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.AsmDefErrors_Text), true)]
    public class AsmDefErrors
    {
    }

    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.AsmDefWarnings_Text), true)]
    public class AsmDefWarnings
    {
    }
}