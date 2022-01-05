using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon.Attributes
{
    // Rider doesn't support an empty set of attributes (all the implementations of IRiderHighlighterModelCreator
    // return null), so we must define something. If we define an EffectType, ReSharper throws if we don't define it
    // properly. But this is just a tooltip, and should have EffectType.NONE. So define one dummy attribute, this keeps
    // both Rider and ReSharper happy
    [RegisterHighlighter(GUID_REFERENCE_TOOLTIP, FontFamily = "Unused")]
    public static class AsmDefHighlightingAttributeIds
    {
        public const string GUID_REFERENCE_TOOLTIP = "ReSharper AsmDef GUID Reference Tooltip";
    }
}