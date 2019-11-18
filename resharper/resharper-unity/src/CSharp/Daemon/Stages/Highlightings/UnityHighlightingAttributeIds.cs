using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

[assembly: RegisterConfigurableHighlightingsGroup(UnityHighlightingGroupIds.Unity, "Unity")]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // Any highlighting attribute that is to be configurable should start with "ReSharper Unity ". This will display
    // correctly in the Visual Studio Fonts & Colors dialog (in full, "ReSharper Unity Attribute ID") and in Rider's
    // Color Scheme settings (by stripping the "ReSharper Unity " prefix).
    // See UnityHighlighterNamesProvider for more details
    public static class UnityHighlightingAttributeIds
    {
        public const string UNITY_GUTTER_ICON_ATTRIBUTE = "Unity Gutter Icon";
        public const string UNITY_PERFORMANCE_CRITICAL_GUTTER_ICON_ATTRIBUTE = "Unity Performance Critical Icon Gutter Icon";
        public const string UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE = "ReSharper Unity Implicitly Used Identifier";
    }

    public static class UnityHighlightingGroupIds
    {
        public const string Unity = "Unity";
        public const string UnityPerformance = "Unity Performance";
    }

    public static class UnityHighlightingCompoundGroupNames
    {
        public const string PerformanceCriticalCode = "Performance indicators";
    }
}
