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
]

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [StaticSeverityHighlighting(
        Severity.INFO, CSharpLanguage.Name,
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeInvocationHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        public readonly IInvocationExpression InvocationExpression;
        public readonly IReference Reference;
        public readonly bool IsMoveToStartAvailable;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeInvocation";
        public const string TITLE = "[Performace] Costly method is invoked";
        public const string MESSAGE = "Costly method is invoked from performance context";

        public PerformanceCriticalCodeInvocationHighlighting(IInvocationExpression invocationExpression, IReference reference, bool isMoveToStartAvailable) : 
            base(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION, MESSAGE)
        {
            InvocationExpression = invocationExpression;
            Reference = reference;
            IsMoveToStartAvailable = isMoveToStartAvailable;
        }

        public override bool IsValid() => Reference.IsValid();

        public override DocumentRange CalculateRange() => Reference.GetDocumentRange();
    }
    
    [StaticSeverityHighlighting(
        Severity.INFO, CSharpLanguage.Name,
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeNullComparisonHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        private readonly IReference myReference;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeNullComparison";
        public const string TITLE = "[Performance] Null comparison in expensive";
        public const string MESSAGE = "Null comparisons against UnityEngine.Object subclasses is expensive";

        public PerformanceCriticalCodeNullComparisonHighlighting(IReference reference) :
            base(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION, MESSAGE)
        {
            myReference = reference;
        }

        public override bool IsValid() => myReference.IsValid();

        public override DocumentRange CalculateRange() => myReference.GetDocumentRange();
    }
    
    [StaticSeverityHighlighting(
        Severity.INFO, CSharpLanguage.Name,
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.CAMERA_MAIN,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeCameraMainHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        public readonly IReferenceExpression ReferenceExpression;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeCameraMain";
        public const string TITLE = "[Performance] Camera.main is expensive";
        public const string MESSAGE = "Camera.main is slow and does not cache its result. Using Camera.main in frequently called methods is very inefficient. Prefer caching the result in Start() or Awake()";

        public PerformanceCriticalCodeCameraMainHighlighting(IReferenceExpression referenceExpression) :
            base(PerformanceCriticalCodeHighlightingAttributeIds.CAMERA_MAIN, MESSAGE)
        {
            ReferenceExpression = referenceExpression;
        }

        public override bool IsValid() => ReferenceExpression.IsValid();

        public override DocumentRange CalculateRange() => ReferenceExpression.GetHighlightingRange();
    }
    
    [StaticSeverityHighlighting(
        Severity.INFO, CSharpLanguage.Name,
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    [DaemonTooltipProvider(typeof(PerformanceCriticalCodeHighlightingTooltipProvider))]
    public class PerformanceCriticalCodeHighlighting: ICustomAttributeIdHighlighting
    {
        private readonly DocumentRange myRange;
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeHighlighting";
        public const string TITLE = "";
        public const string MESSAGE = "";

        public PerformanceCriticalCodeHighlighting(DocumentRange range)
        {
            myRange = range;
        }

        public bool IsValid() => true;

        public DocumentRange CalculateRange() => myRange;

        public string ToolTip => string.Empty;
        public string ErrorStripeToolTip => string.Empty;
        public string AttributeId => PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_HIGHLIGHTER;
    }
    
    
    public abstract class PerformanceCriticalCodeHighlightingBase : ICustomAttributeIdHighlighting
    {
        protected PerformanceCriticalCodeHighlightingBase([NotNull] string attributeId, [NotNull] string message)
        {
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
}