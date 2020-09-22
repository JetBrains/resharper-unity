using JetBrains.ReSharper.Host.Features.RunMarkers;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Rider.Host.Features.RunMarkers
{
  [
    RegisterHighlighter(
      UnityRunMarkerAttributeIds.RUN_METHOD_MARKER_ID,
      Layer = HighlighterLayer.SYNTAX + 1,
      EffectType = EffectType.GUTTER_MARK,
      GutterMarkType = typeof(StaticMethodRunMarkerGutterMark)
    )
  ]
  public static class UnityRunMarkerAttributeIds
  {
    public const string RUN_METHOD_MARKER_ID = "Unity Run Method Gutter Mark";
  }
}