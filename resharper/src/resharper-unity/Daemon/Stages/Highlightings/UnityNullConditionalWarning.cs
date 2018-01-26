using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(UnityNullConditionalWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, UnityNullConditionalWarning.MESSAGE,
    "Unity Object types can't be compared using null conditionals. Use ternary operator instead.",
    Severity.ERROR)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.ERROR,
        ToolTipFormatString = MESSAGE)]
    public class UnityNullConditionalWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.NoNullConditonal";
        public const string MESSAGE = "Unity Object types can't be compared using null conditionals.";

        public UnityNullConditionalWarning(IConditionalAccessExpression expression)
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
        public IConditionalAccessExpression Expression { get; }
    }
}
