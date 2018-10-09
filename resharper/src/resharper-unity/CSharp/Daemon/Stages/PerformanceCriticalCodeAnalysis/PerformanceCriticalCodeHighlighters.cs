using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.BuildScripts.DaemonStage.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
[assembly:
    RegisterConfigurableSeverity(
        PerformanceCriticalCodeInvocationHighlighting.SEVERITY_ID, 
        null, 
        PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        "Expensive method call in frequently called method",
        "Expensive method call in frequently called method", 
        Severity.INFO
        ),
    RegisterConfigurableSeverity(
        PerformanceCriticalCodeInvocationReachableHighlighting.SEVERITY_ID, 
        null, 
        PerformanceCriticalCodeHighlightingAttributeIds.GroupID,
        "Expensive method is indirectly invoked from the frequently called method",
        "Expensive method is indirectly invoked from the frequently called method",
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
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeInvocation";
        public const string MESSAGE = "Costly method is invoked from performance context";

        public PerformanceCriticalCodeInvocationHighlighting(IReference reference) : 
            base(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_INVOCATION, reference, MESSAGE) { }
    }
    
    [StaticSeverityHighlighting(
        Severity.INFO, CSharpLanguage.Name,
        AttributeId = PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_REACHABLE,
        ShowToolTipInStatusBar = false,
        ToolTipFormatString = MESSAGE)]
    public class PerformanceCriticalCodeInvocationReachableHighlighting : PerformanceCriticalCodeHighlightingBase
    {
        public const string SEVERITY_ID = "Unity.PerformanceCriticalCodeInvocationReachable";
        public const string MESSAGE = "Invocation of this method indirectly calls costly method";

        public PerformanceCriticalCodeInvocationReachableHighlighting(IReference reference) :
            base(PerformanceCriticalCodeHighlightingAttributeIds.COSTLY_METHOD_REACHABLE,reference, MESSAGE) { }
    }
    
    
    public abstract class PerformanceCriticalCodeHighlightingBase : ICustomAttributeIdHighlighting
    {
        private readonly IReference myReference;

        protected PerformanceCriticalCodeHighlightingBase([NotNull] string attributeId, [NotNull] IReference reference, [NotNull] string message)
        {
            myReference = reference;
            ToolTip = message;
            AttributeId = attributeId;
        }

        public bool IsValid() => myReference.IsValid();
        public DocumentRange CalculateRange() => myReference.GetDocumentRange();

        [NotNull] public string ToolTip { get; }
        [NotNull] public string ErrorStripeToolTip => ToolTip;
        public string AttributeId { get; }
    }
}