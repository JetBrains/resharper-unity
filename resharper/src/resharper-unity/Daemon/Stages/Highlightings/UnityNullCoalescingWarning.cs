using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(UnityNullCoalescingWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, UnityNullCoalescingWarning.MESSAGE,
    "Unity Object types can't be compared using null coalescing. Use ternary operator instead.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class UnityNullCoalescingWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.NoNullCoalescing";
        public const string MESSAGE = "Unity Object types can't be compared using null coalescing.";

        public UnityNullCoalescingWarning(INullCoalescingExpression expression)
        {
            Expression = expression;
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
        public INullCoalescingExpression Expression { get; }
    }
}
