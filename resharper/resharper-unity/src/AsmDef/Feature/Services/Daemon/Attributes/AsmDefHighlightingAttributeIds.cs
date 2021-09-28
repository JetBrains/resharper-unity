using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon.Attributes
{
    [RegisterHighlighter(GUID_REFERENCE_TOOLTIP, EffectType = EffectType.TEXT)]
    public static class AsmDefHighlightingAttributeIds
    {
        public const string GUID_REFERENCE_TOOLTIP = "ReSharper AsmDef GUID Reference Tooltip";
    }
}