using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using Strings = JetBrains.ReSharper.Plugins.Unity.Resources.Strings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;

[DaemonAdornmentProvider(typeof(UnityObjectLifetimeCheckViaNullEqualityHintAdornmentProvider))]
[DaemonTooltipProvider(typeof(InlayHintTooltipProvider))]
[StaticSeverityHighlighting(Severity.INFO,
    typeof(HighlightingGroupIds.IntraTextAdornments),
    AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
    OverlapResolve = OverlapResolveKind.NONE,
    ShowToolTipInStatusBar = false)]
public class UnityObjectLifetimeCheckViaNullEqualityHintHighlighting(IEqualityExpression expression) : IUnityHighlighting, IInlayHintWithDescriptionHighlighting, IHighlightingWithTestOutput
{
    public const double DefaultOrder = 2;

    public IEqualityExpression Expression => expression;
    public IconId Icon => PsiSymbolsThemedIcons.InterceptedCall.Id;
    public /*Localized*/ string ToolTip => Strings.UnityObjectLifetimeCheckViaNullEqualityHint_Tooltip;
    public /*Localized*/ string ErrorStripeToolTip => ToolTip;
    public bool IsValid() => expression.IsValid();
    public DocumentRange CalculateRange() => expression.OperatorSign.GetHighlightingRange();

    public RichText Text { get; } = new(string.Empty);
    public /*Localized*/ RichText Description { get; } = new(Strings.UnityObjectLifetimeCheckViaNullEqualityHint_Description); 
    public string TestOutput => $"🖼️{Icon}|🏷️{Text.Text}|📖{Description}"; 
}