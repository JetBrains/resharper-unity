using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages;
using JetBrains.TextControl.DocumentMarkup;

[assembly:
    
    RegisterConfigurableHighlightingsGroup(PerformanceCriticalCodeHighlightingAttributeIds.GroupID, "Unity Costly Highlighters"),
    RegisterHighlighter(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_REACHABLE,
        GroupId = PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ffd070",
        Layer = HighlighterLayer.SYNTAX + 1),
    RegisterHighlighter(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        GroupId = PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        EffectType = EffectType.SOLID_UNDERLINE,
        EffectColor = "#ff7526",
        Layer = HighlighterLayer.SYNTAX + 1),
    
]
namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    
    public static class PerformanceCriticalCodeHighlightingAttributeIds
    {
        public const string GroupID = "ReSharper Unity CostlyHighlighters";
        
        public const string COSTLY_METHOD_REACHABLE = "Resharper Unity CostlyMethodReachable";
        public const string COSTLY_METHOD_INVOCATION = "Resharper Unity CostlyMethodInvocation";
    }
}