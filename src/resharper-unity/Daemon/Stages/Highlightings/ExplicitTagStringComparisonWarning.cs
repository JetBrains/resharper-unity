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
    public class ExplicitTagStringComparisonWarning : IHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.ExplicitTagComparison";
        public const string MESSAGE = "Explicit string comparison is inefficient, use CompareTag instead";

        private readonly IEqualityExpression myExpression;

        public ExplicitTagStringComparisonWarning(IEqualityExpression expression)
        {
            myExpression = expression;
        }

        public bool IsValid()
        {
            return myExpression != null && myExpression.IsValid();
        }

        public DocumentRange CalculateRange()
        {
            return myExpression.GetHighlightingRange();
        }

        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}