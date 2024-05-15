using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Resources;

// todo:Localization?

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors
{
    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.UnityErrors_Text), true)]
    public class UnityErrors
    {
    }

    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.UnityWarnings_Text), true)]
    public class UnityWarnings
    {
    }

    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.UnityGutterMarks_Text), true)]
    public class UnityGutterMarks
    {
    }
}