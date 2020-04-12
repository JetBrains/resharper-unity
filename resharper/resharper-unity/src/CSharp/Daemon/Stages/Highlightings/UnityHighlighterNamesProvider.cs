using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

// Displayed as a group in Rider's Code Style. Not used by ReSharper
[assembly: RegisterHighlighterGroup(UnityHighlightingGroupIds.Unity, "Unity",
    HighlighterGroupPriority.LANGUAGE_SETTINGS,
    RiderNamesProviderType = typeof(UnityHighlighterNamesProvider))]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    // This will be used to populate the names in the "Unity" group in Rider's Code Style settings. It will strip off
    // any leading "ReSharper Unity " text, and save any customisations as "UNITY_ATTRIBUTE_ID". So this means all
    // highlighters in the group should begin with "ReSharper Unity ", and they will display nicely/correctly in both
    // Rider and Visual Studio (which uses the full highlighter text - "ReSharper Unity Attribute ID")
    public class UnityHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        public UnityHighlighterNamesProvider()
            : base("ReSharper Unity", "UNITY")
        {
        }

        public override string GetExternalName(string attributeId)
        {
            return GetHighlighterTag(attributeId);
        }
    }
}