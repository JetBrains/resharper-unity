using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(UnityNullPropagationWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, UnityNullPropagationWarning.MESSAGE,
    "Unity Object properties can't be accessed via null propagation, use conditional access instead.",
    Severity.ERROR)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.ERROR,
        ToolTipFormatString = MESSAGE)]
    public class UnityNullPropagationWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.NoNullPropagation";
        public const string MESSAGE = "Unity Object properties can't be accessed via null propagation.";

        public UnityNullPropagationWarning(IConditionalAccessExpression expression)
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
