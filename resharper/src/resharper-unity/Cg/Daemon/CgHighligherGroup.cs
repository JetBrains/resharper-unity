using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighterGroup(
    CgHighligherGroup.ID, "Cg/HLSL", HighlighterGroupPriority.LANGUAGE_SETTINGS,
    RiderNamesProviderType = typeof(CgHighlighterNamesProvider))]

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon
{
    public static class CgHighligherGroup
    {
        public const string ID = "ReSharper Cg Highlighters";
    }

    public class CgHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        public CgHighlighterNamesProvider() : base("ReSharper Cg", "ReSharper.Cg")
        {
        }
    }
}