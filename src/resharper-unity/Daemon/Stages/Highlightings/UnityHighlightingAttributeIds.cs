using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;

[assembly: RegisterConfigurableHighlightingsGroup(UnityHighlightingGroupIds.Unity, "Unity")]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    public static class UnityHighlightingAttributeIds
    {
        public const string UNITY_GUTTER_ICON_ATTRIBUTE = "Unity Gutter Icon";
    }

    public static class UnityHighlightingGroupIds
    {
        public const string Unity = "Unity";
    }
}