using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.UnityPerformanceHints_Text), true)]
    public class UnityPerformanceHighlighting
    {
    }
    
    [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.UnityBurst_Text), true)]
    public class UnityBurstHighlighting
    {
    }
}