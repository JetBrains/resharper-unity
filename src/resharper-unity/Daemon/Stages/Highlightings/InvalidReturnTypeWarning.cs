using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

[assembly: RegisterConfigurableSeverity(InvalidReturnTypeWarning.HIGHLIGHTING_ID,
    UnityHighlightingGroupIds.INCORRECT_EVENT_FUNCTION_SIGNATURE,
    UnityHighlightingGroupIds.Unity, InvalidReturnTypeWarning.MESSAGE,
    "Incorrect return type for Unity event function.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    // TODO: Check out CSharpConflictDeclarationsContextSearch
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class InvalidReturnTypeWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.InvalidReturnType";
        public const string MESSAGE = "Incorrect return type for Unity event function";

        public InvalidReturnTypeWarning(IMethodDeclaration methodDeclaration, UnityEventFunction function)
        {
            Function = function;
            MethodDeclaration = methodDeclaration;
        }

        public bool IsValid()
        {
            return MethodDeclaration != null && MethodDeclaration.IsValid();
        }

        public DocumentRange CalculateRange()
        {
            var nameRange = MethodDeclaration.GetNameDocumentRange();
            if (!nameRange.IsValid())
                return DocumentRange.InvalidRange;

            var returnType = MethodDeclaration.TypeUsage;
            if (returnType == null)
                return nameRange;

            var documentRange = returnType.GetDocumentRange();
            return !documentRange.IsValid() ? nameRange : documentRange;
        }

        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;

        public UnityEventFunction Function { get; }
        public IMethodDeclaration MethodDeclaration { get; }
    }
}