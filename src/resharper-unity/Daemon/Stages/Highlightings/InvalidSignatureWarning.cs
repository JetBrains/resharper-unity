using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

[assembly: RegisterConfigurableSeverity(InvalidSignatureWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, InvalidSignatureWarning.MESSAGE,
    "Incorrect signature for the given Unity event function.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    // TODO: Check out CSharpConflictDeclarationsContextSearch
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class InvalidSignatureWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.InvalidSignature";
        public const string MESSAGE = "Incorrect signature for Unity event function";

        private readonly IMethodDeclaration myMethodDeclaration;

        public InvalidSignatureWarning(IMethodDeclaration methodDeclaration, UnityEventFunction function)
        {
            myMethodDeclaration = methodDeclaration;
        }

        public bool IsValid()
        {
            return myMethodDeclaration != null && myMethodDeclaration.IsValid();
        }

        public DocumentRange CalculateRange()
        {
            var nameRange = myMethodDeclaration.GetNameDocumentRange();
            if (!nameRange.IsValid())
                return DocumentRange.InvalidRange;

            var @params = myMethodDeclaration.Params;
            if (@params == null)
                return nameRange;

            var paramsRange = @params.GetDocumentRange();
            if (!paramsRange.IsValid())
                return nameRange;

            if (!paramsRange.IsEmpty)
                return paramsRange;

            var lparRange = myMethodDeclaration.LPar?.GetDocumentRange();
            var rparRange = myMethodDeclaration.RPar?.GetDocumentRange();
            var startOffset = lparRange != null && lparRange.Value.IsValid()
                ? lparRange.Value
                : paramsRange;
            var endOffset = rparRange != null && rparRange.Value.IsValid()
                ? rparRange.Value
                : paramsRange;

            return new DocumentRange(startOffset.StartOffset, endOffset.EndOffset);
        }

        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}