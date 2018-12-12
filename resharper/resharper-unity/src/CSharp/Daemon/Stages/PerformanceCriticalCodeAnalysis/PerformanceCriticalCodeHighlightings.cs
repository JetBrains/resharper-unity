using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.TextControl.DocumentMarkup.LineMarkers;

[assembly:
    RegisterConfigurableSeverity(
        PerformanceInvocationHighlighting.SEVERITY_ID, 
        null, 
        UnityHighlightingGroupIds.Unity,
        PerformanceInvocationHighlighting.TITLE,
        PerformanceInvocationHighlighting.MESSAGE, 
        Severity.INFO
        ),
    RegisterConfigurableSeverity(
        PerformanceNullComparisonHighlighting.SEVERITY_ID, 
        null, 
        UnityHighlightingGroupIds.Unity,
        PerformanceNullComparisonHighlighting.TITLE,
        PerformanceNullComparisonHighlighting.MESSAGE,
        Severity.INFO
    ),
    RegisterConfigurableSeverity(
        PerformanceHighlighting.SEVERITY_ID, 
        null, 
        UnityHighlightingGroupIds.Unity,
        PerformanceHighlighting.TITLE,
        PerformanceHighlighting.MESSAGE,
        Severity.INFO
    ),
    RegisterConfigurableSeverity(
        PerformanceCameraMainHighlighting.SEVERITY_ID, 
        null, 
        UnityHighlightingGroupIds.Unity,
        PerformanceCameraMainHighlighting.TITLE,
        PerformanceCameraMainHighlighting.MESSAGE,
        Severity.INFO
    ),
]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [ConfigurableSeverityHighlighting(
        PerformanceInvocationHighlighting.SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceInvocationHighlighting : PerformanceHighlightingBase
    {
        [CanBeNull] public readonly IInvocationExpression InvocationExpression;
        [NotNull] public readonly IReference Reference;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeInvocation";
        public const string TITLE = "[Performace] Costly method is invoked";
        public const string MESSAGE = "Costly method is invoked from performance context";

        public PerformanceInvocationHighlighting([CanBeNull] IInvocationExpression invocationExpression, [NotNull] IReference reference) : 
            base(SEVERITY_ID, PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION, MESSAGE)
        {
            InvocationExpression = invocationExpression;
            Reference = reference;
        }

        public override bool IsValid() => Reference.IsValid();

        public override DocumentRange CalculateRange() => Reference.GetDocumentRange();
    }
    
    [ConfigurableSeverityHighlighting(
        PerformanceNullComparisonHighlighting.SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.NULL_COMPARISON,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceNullComparisonHighlighting : PerformanceHighlightingBase
    {
        [NotNull] public ICSharpExpression Expression;
        [NotNull] public string FieldName;
        [NotNull] private readonly IReference myReference;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeNullComparison";
        public const string TITLE = "[Performance] Null comparison in expensive";
        public const string MESSAGE = "Null comparisons against UnityEngine.Object subclasses is expensive";

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
    
    [ConfigurableSeverityHighlighting(
        PerformanceCameraMainHighlighting.SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.CAMERA_MAIN,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCameraMainHighlighting : PerformanceHighlightingBase
    {
        [NotNull] public readonly IReferenceExpression ReferenceExpression;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeCameraMain";
        public const string TITLE = "[Performance] Camera.main is expensive";
        public const string MESSAGE = "Camera.main is slow and does not cache its result. Using Camera.main in frequently called methods is very inefficient. Prefer caching the result in Start() or Awake()";

        public PerformanceCameraMainHighlighting([NotNull] IReferenceExpression referenceExpression) :
            base(SEVERITY_ID, PerformanceHighlightingAttributeIds.CAMERA_MAIN, MESSAGE)
        {
            ReferenceExpression = referenceExpression;
        }

        public override bool IsValid() => ReferenceExpression.IsValid();

        public override DocumentRange CalculateRange() => ReferenceExpression.GetHighlightingRange();
    }
    
    [StaticSeverityHighlighting(Severity.INFO, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    [DaemonTooltipProvider(typeof(PerformanceCriticalCodeHighlightingTooltipProvider))]
    public class PerformanceHighlighting: PerformanceHighlightingBase, IActiveLineMarkerInfo
    {
        private readonly DocumentRange myRange;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeHighlighting";
        public const string TITLE = "";
        public const string MESSAGE = "";

        public PerformanceHighlighting(DocumentRange range)
            : base(SEVERITY_ID, PerformanceHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER, MESSAGE)
        {
            myRange = range;
        }

        public override bool IsValid() => true;
        public override DocumentRange CalculateRange() => myRange;

        public string RendererId => null;
        public int Thickness => 3;
        public LineMarkerPosition Position => LineMarkerPosition.LEFT;
        public ExecutableItem LeftClick() => null;

        public string Tooltip => "Performance critical context";
    }
    
    
    public abstract class PerformanceHighlightingBase : ICustomAttributeIdHighlighting, IUnityHighlighting
    {
        [NotNull] public readonly string SeverityId;

        protected PerformanceHighlightingBase([NotNull] string severityId, [NotNull] string attributeId, [NotNull] string message)
        {
            SeverityId = severityId;
            ToolTip = message;
            AttributeId = attributeId;
        }

        public abstract bool IsValid();
        public abstract DocumentRange CalculateRange();

        [NotNull] public string ToolTip { get; }
        [NotNull] public string ErrorStripeToolTip => ToolTip;
        public string AttributeId { get; }
    }
    
    [SolutionComponent]
    public class PerformanceCriticalCodeHighlightingTooltipProvider : IdentifierTooltipProvider<CSharpLanguage>
    {
        public PerformanceCriticalCodeHighlightingTooltipProvider(Lifetime lifetime, ISolution solution, IDeclaredElementDescriptionPresenter presenter)
            : base(lifetime, solution, presenter)
        {
        }
    }
   
    public static class PerformanceHighlightingAttributeIds
    { 
        public const string CAMERA_MAIN = "ReSharper Unity PerformanceCameraMain";
        public const string COSTLY_METHOD_INVOCATION = "ReSharper Unity PerformanceCostlyMethodInvocation";
        public const string NULL_COMPARISON = "ReSharper Unity PerformanceNullComparison";
        public const string COSTLY_METHOD_HIGHLIGHTER = "ReSharper Unity PerformanceCostlyMethodHighlighter";
    }
}