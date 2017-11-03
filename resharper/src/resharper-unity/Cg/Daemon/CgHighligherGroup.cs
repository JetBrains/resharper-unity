using JetBrains.ReSharper.Plugins.Unity.Cg.Daemon;
using JetBrains.TextControl.DocumentMarkup;

[assembly: RegisterHighlighterGroup(
    CgHighligherGroup.ID, "Cg/HLSL", HighlighterGroupPriority.LANGUAGE_SETTINGS,
    DemoText = CgHighligherGroup.DEMO_TEXT,
    RiderNamesProviderType = typeof(CgHighlighterNamesProvider))]

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon
{
    public static class CgHighligherGroup
    {
        public const string ID = "ReSharper Cg Highlighters";
        public const string DEMO_TEXT = null;
    }

    public class CgHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        public CgHighlighterNamesProvider() : base("ReSharper Cg", "ReSharper.Cg")
        {
        }
    }
}