using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;

[RegisterHighlighter(
     ID,
     EffectType = EffectType.NONE,
     GroupId = HighlighterGroupIds.HIDDEN,
     Layer = HighlighterLayer.SELECTION,
     TransmitUpdates = true),
 StaticSeverityHighlighting(
     Severity.INFO,
     typeof(UnityProfilerHighlighting),
     OverlapResolve = OverlapResolveKind.NONE,
     ShowToolTipInStatusBar = false
 )]
public class UnityProfilerHighlighting(DocumentRange range, ModelUnityProfilerSampleInfo sampleInfo): ICustomAttributeIdHighlighting
{
    private const string ID = "Unity Profiler Highlighting ID";

    public ModelUnityProfilerSampleInfo SampleInfo => sampleInfo;

    public string? ToolTip => null;
    public string? ErrorStripeToolTip => null;
    public string AttributeId => ID;
  
    public bool IsValid() => range.IsValid();

    public DocumentRange CalculateRange() => range;
}