using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.TextControl.DocumentMarkup.LineMarkers;

[assembly:
    RegisterConfigurableSeverity(
        PerformanceCriticalCodeInvocationHighlighting.SEVERITY_ID, 
        null, 
        PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        PerformanceCriticalCodeInvocationHighlighting.TITLE,
        PerformanceCriticalCodeInvocationHighlighting.MESSAGE, 
        Severity.INFO
        ),
    RegisterConfigurableSeverity(
        PerformanceCriticalCodeNullComparisonHighlighting.SEVERITY_ID, 
        null, 
        PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        PerformanceCriticalCodeNullComparisonHighlighting.TITLE,
        PerformanceCriticalCodeNullComparisonHighlighting.MESSAGE,
        Severity.INFO
    ),
    RegisterConfigurableSeverity(
        PerformanceCriticalCodeHighlighting.SEVERITY_ID, 
        null, 
        PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        PerformanceCriticalCodeHighlighting.TITLE,
        PerformanceCriticalCodeHighlighting.MESSAGE,
        Severity.INFO
    ),
    RegisterConfigurableSeverity(
        PerformanceCriticalCodeCameraMainHighlighting.SEVERITY_ID, 
        null, 
        PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        PerformanceCriticalCodeCameraMainHighlighting.TITLE,
        PerformanceCriticalCodeCameraMainHighlighting.MESSAGE,
        Severity.INFO
    ),
    
    RegisterConfigurableHighlightingsGroup(PerformanceCriticalCodeHighlightingAttributeIds.GroupID, "Unity performance analysis")
]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [ConfigurableSeverityHighlighting(
        PerformanceCriticalCodeInvocationHighlighting.SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeInvocationHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        [CanBeNull] public readonly IInvocationExpression InvocationExpression;
        [NotNull] public readonly IReference Reference;
        public readonly bool IsMoveToStartAvailable;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeInvocation";
        public const string TITLE = "[Performace] Costly method is invoked";
        public const string MESSAGE = "Costly method is invoked from performance context";

        public PerformanceCriticalCodeInvocationHighlighting([CanBeNull] IInvocationExpression invocationExpression, [NotNull] IReference reference, bool isMoveToStartAvailable) : 
            base(SEVERITY_ID, PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION, MESSAGE)
        {
            InvocationExpression = invocationExpression;
            Reference = reference;
            IsMoveToStartAvailable = isMoveToStartAvailable;
        }

        public override bool IsValid() => Reference.IsValid();

        public override DocumentRange CalculateRange() => Reference.GetDocumentRange();
    }
    
    [ConfigurableSeverityHighlighting(
        PerformanceCriticalCodeNullComparisonHighlighting.SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.NULL_COMPARISON,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeNullComparisonHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        [NotNull] public ICSharpExpression Expression;
        [NotNull] public string FieldName;
        [NotNull] private readonly IReference myReference;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeNullComparison";
        public const string TITLE = "[Performance] Null comparison in expensive";
        public const string MESSAGE = "Null comparisons against UnityEngine.Object subclasses is expensive";

        public PerformanceCriticalCodeNullComparisonHighlighting([NotNull] ICSharpExpression expression, [NotNull] string fieldName, [NotNull] IReference reference) :
            base(SEVERITY_ID, PerformanceCriticalCodeHighlightingAttributeIds.NULL_COMPARISON, MESSAGE)
        {
            Expression = expression;
            FieldName = fieldName;
            myReference = reference;
        }

        public override bool IsValid() => myReference.IsValid();

        public override DocumentRange CalculateRange() => myReference.GetDocumentRange();
    }
    
    [ConfigurableSeverityHighlighting(
        PerformanceCriticalCodeCameraMainHighlighting.SEVERITY_ID, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.CAMERA_MAIN,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeCameraMainHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        [NotNull] public readonly IReferenceExpression ReferenceExpression;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeCameraMain";
        public const string TITLE = "[Performance] Camera.main is expensive";
        public const string MESSAGE = "Camera.main is slow and does not cache its result. Using Camera.main in frequently called methods is very inefficient. Prefer caching the result in Start() or Awake()";

        public PerformanceCriticalCodeCameraMainHighlighting([NotNull] IReferenceExpression referenceExpression) :
            base(SEVERITY_ID, PerformanceCriticalCodeHighlightingAttributeIds.CAMERA_MAIN, MESSAGE)
        {
            ReferenceExpression = referenceExpression;
        }

        public override bool IsValid() => ReferenceExpression.IsValid();

        public override DocumentRange CalculateRange() => ReferenceExpression.GetHighlightingRange();
    }
    
    [StaticSeverityHighlighting(Severity.INFO, CSharpLanguage.Name, Languages = "CSHARP",
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    [DaemonTooltipProvider(typeof(PerformanceCriticalCodeHighlightingTooltipProvider))]
    public class PerformanceCriticalCodeHighlighting: PerformanceCriticalCodeHighlightingBase, ILineMarkerInfo
    {
        private readonly DocumentRange myRange;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeHighlighting";
        public const string TITLE = "";
        public const string MESSAGE = "";

        public PerformanceCriticalCodeHighlighting(DocumentRange range)
            : base(SEVERITY_ID, PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER, MESSAGE)
        {
            myRange = range;
        }

        public override bool IsValid() => true;
        public override DocumentRange CalculateRange() => myRange;

        public string RendererId => null;
        public int Thickness => 3;
        public LineMarkerPosition Position => LineMarkerPosition.LEFT;
    }
    
    
    public abstract class PerformanceCriticalCodeHighlightingBase : ICustomAttributeIdHighlighting
    {
        [NotNull] public readonly string SeverityId;

        protected PerformanceCriticalCodeHighlightingBase([NotNull] string severityId, [NotNull] string attributeId, [NotNull] string message)
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
   
    public static class PerformanceCriticalCodeHighlightingAttributeIds
    {
        public const string GroupID = "ReSharper Unity PerformanceAnalysisHighlighters";
        
        public const string CAMERA_MAIN = "ReSharper Unity PerformanceCameraMain";
        public const string COSTLY_METHOD_INVOCATION = "ReSharper Unity PerformanceCostlyMethodInvocation";
        public const string NULL_COMPARISON = "ReSharper Unity PerformanceNullComparison";
        public const string COSTLY_METHOD_HIGHLIGHTER = "ReSharper Unity PerformanceCostlyMethodHighlighter";
    }
}