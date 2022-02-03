using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.RunMarkers
{
    [
        RegisterHighlighter(
            RUN_METHOD_MARKER_ID,
            Layer = HighlighterLayer.SYNTAX + 1,
            EffectType = EffectType.GUTTER_MARK,
            GutterMarkType = typeof(UnityStaticMethodRunMarkerGutterMark)
        )
    ]
    public static class UnityRunMarkerAttributeIds
    {
        public const string RUN_METHOD_MARKER_ID = "Unity Run Method Gutter Mark";
    }
}