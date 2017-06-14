using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    // Unity will give a compile time error for this for SyncVar
    [StaticSeverityHighlighting(Severity.ERROR, UnityHighlightingGroupIds.Unity,
        OverlapResolve = OverlapResolveKind.ERROR,
        ToolTipFormatString = MESSAGE)]
    public class StringLiteralReferenceIncorrectSignatureError : IHighlighting, IUnityHighlighting
    {
        private const string MESSAGE = "Method has incorrect signature";

        public StringLiteralReferenceIncorrectSignatureError(IReference reference)
        {
            Reference = reference;
        }

        public IReference Reference { get; }

        public bool IsValid()
        {
            return Reference == null || Reference.IsValid();
        }

        public DocumentRange CalculateRange() => Reference.GetDocumentRange();

        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}