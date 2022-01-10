using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Pencils;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // RegisterConfigurableHighlightingsGroup registers a group in Inspection Severity
    [RegisterConfigurableHighlightingsGroup(Unity, "Unity")]
    [RegisterConfigurableHighlightingsGroup(UnityPerformance, "Unity Performance Inspections", PencilsGroupKind.UnityPerformanceKind)]
    [RegisterConfigurableHighlightingsGroup(Burst, "Unity Burst Compiler Warnings")]
    public static class UnityHighlightingGroupIds
    {
        public const string Unity = "UNITY";
        public const string UnityPerformance = "UNITY_PERFORMANCE";
        public const string Burst = "UNITY_BURST";
    }
}