using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Feature.HighlightingEye;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.LineMarkers;

[assembly:
    RegisterConfigurableSeverity(
        PerformanceInvocationHighlighting.SEVERITY_ID,
        UnityHighlightingCompoundGroupNames.PerformanceCriticalCode,
        UnityHighlightingGroupIds.UnityPerformance,
        PerformanceInvocationHighlighting.TITLE,
        PerformanceInvocationHighlighting.DESCRIPTION,
        Severity.HINT
    ),
    RegisterConfigurableSeverity(
        PerformanceNullComparisonHighlighting.SEVERITY_ID,
        UnityHighlightingCompoundGroupNames.PerformanceCriticalCode,
        UnityHighlightingGroupIds.UnityPerformance,
        PerformanceNullComparisonHighlighting.TITLE,
        PerformanceNullComparisonHighlighting.DESCRIPTION,
        Severity.HINT
    ),
    RegisterConfigurableSeverity(
        PerformanceCameraMainHighlighting.SEVERITY_ID,
        UnityHighlightingCompoundGroupNames.PerformanceCriticalCode,
        UnityHighlightingGroupIds.UnityPerformance,
        PerformanceCameraMainHighlighting.TITLE,
        PerformanceCameraMainHighlighting.DESCRIPTION,
        Severity.HINT
    ),
    RegisterConfigurableHighlightingsGroup(
        UnityHighlightingGroupIds.UnityPerformance,
        "Unity Performance Inspections",
        HighlightingEyeGroupKind.UnityPerformanceKind
        )
]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name,
        AttributeId = PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceInvocationHighlighting : PerformanceHighlightingBase
    {
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeInvocation";
        public const string TITLE = "Expensive method invocation";
        public const string DESCRIPTION = "This method call is inefficient when called inside a performance critical context.";
        public const string MESSAGE = "Expensive method invocation";

        [CanBeNull] public readonly IInvocationExpression InvocationExpression;
        [NotNull] public readonly IReference Reference;

        public PerformanceInvocationHighlighting([CanBeNull] IInvocationExpression invocationExpression, [NotNull] IReference reference) :
            base(SEVERITY_ID, PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION, MESSAGE)
        {
            InvocationExpression = invocationExpression;
            Reference = reference;
        }

        public override bool IsValid() => Reference.IsValid();
        public override DocumentRange CalculateRange() => Reference.GetDocumentRange();
    }

    [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.NULL_COMPARISON,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceNullComparisonHighlighting : PerformanceHighlightingBase
    {
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeNullComparison";
        public const string TITLE = "Expensive null comparison";
        public const string DESCRIPTION = "Equality operations on objects deriving from 'UnityEngine.Object' will also check that the underlying native object has not been destroyed. This requires a call into native code and can have a performance impact when used inside frequently called methods.";
        public const string MESSAGE = "Comparison to 'null' is expensive";

        [NotNull] public ICSharpExpression Expression;
        [NotNull] public string FieldName;
        [NotNull] private readonly IReference myReference;

        public PerformanceNullComparisonHighlighting([NotNull] ICSharpExpression expression, [NotNull] string fieldName, [NotNull] IReference reference) :
            base(SEVERITY_ID, PerformanceHighlightingAttributeIds.NULL_COMPARISON, MESSAGE)
        {
            Expression = expression;
            FieldName = fieldName;
            myReference = reference;
        }

        public override bool IsValid() => myReference.IsValid();
        public override DocumentRange CalculateRange() => myReference.GetDocumentRange();
    }

    [ConfigurableSeverityHighlighting(SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.CAMERA_MAIN,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCameraMainHighlighting : PerformanceHighlightingBase
    {
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeCameraMain";
        public const string TITLE = "'Camera.main' is expensive";
        public const string DESCRIPTION = "'Camera.main' is slow and does not cache its result. Using 'Camera.main' in frequently called methods is very inefficient. Prefer caching the result in 'Start()' or 'Awake()'";
        public const string MESSAGE = "'Camera.main' is expensive";

        [NotNull] public readonly IReferenceExpression ReferenceExpression;

        public PerformanceCameraMainHighlighting([NotNull] IReferenceExpression referenceExpression) :
            base(SEVERITY_ID, PerformanceHighlightingAttributeIds.CAMERA_MAIN, MESSAGE)
        {
            ReferenceExpression = referenceExpression;
        }

        public override bool IsValid() => ReferenceExpression.IsValid();
        public override DocumentRange CalculateRange() => ReferenceExpression.GetHighlightingRange();
    }

    [StaticSeverityHighlighting(Severity.INFO, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceHighlighting: PerformanceHighlightingBase , IActiveLineMarkerInfo
    {
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeHighlighting";
        public const string TITLE = "Performance critical context";
        public const string MESSAGE = "Performance critical context";

        private readonly DocumentRange myRange;

        public PerformanceHighlighting(DocumentRange range)
            : base(SEVERITY_ID, PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER, MESSAGE)
        {
            myRange = range;
        }

        public override bool IsValid() => true;
        public override DocumentRange CalculateRange() => myRange;
        public string RendererId => null;
        public int Thickness => 1;
        public LineMarkerPosition Position =>  LineMarkerPosition.RIGHT;
        public ExecutableItem LeftClick() => null;
        public string Tooltip => "Performance critical context";
    }


    public abstract class PerformanceHighlightingBase : IHighlighting, IUnityHighlighting
    {
        [NotNull] public readonly string SeverityId;

        protected PerformanceHighlightingBase([NotNull] string severityId, [NotNull] string attributeId, [NotNull] string message)
        {
            SeverityId = severityId;
            ToolTip = message;
        }

        public abstract bool IsValid();
        public abstract DocumentRange CalculateRange();

        [NotNull] public string ToolTip { get; }
        [NotNull] public string ErrorStripeToolTip => ToolTip;
    }

    public class PerformanceContextHiglighting : HighlightInfo
    {
        public PerformanceContextHiglighting(DocumentRange documentRange)
            : base(PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER, documentRange, AreaType.EXACT_RANGE, HighlighterLayer.SYNTAX + 1)
        {
        }

        public override IHighlighter CreateHighlighter(IDocumentMarkup markup)
        {
            var highlighter = base.CreateHighlighter(markup);
            highlighter.UserData = new PerformanceHighlighting(DocumentRange.InvalidRange);
            return highlighter;
        }

    }
}