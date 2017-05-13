using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(ExplicitTagStringComparisonWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, ExplicitTagStringComparisonWarning.MESSAGE,
    "Explicit string comparison with GameObject.tag or Component.tag is inefficient. Use the CompareTag method instead.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class ExplicitTagStringComparisonWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.ExplicitTagComparison";
        public const string MESSAGE = "Explicit string comparison is inefficient, use CompareTag instead";

        public ExplicitTagStringComparisonWarning(IEqualityExpression expression, bool leftOperandIsTagReference)
        {
            Expression = expression;
            LeftOperandIsTagReference = leftOperandIsTagReference;
        }

        public bool IsValid()
        {
            return Expression != null && Expression.IsValid();
        }

        public DocumentRange CalculateRange()
        {
            return Expression.GetHighlightingRange();
        }

        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
        public IEqualityExpression Expression { get; }
        public bool LeftOperandIsTagReference { get; }
    }
}