using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

[assembly: RegisterConfigurableSeverity(InvalidReturnTypeWarning.HIGHLIGHTING_ID,
    UnityHighlightingGroupIds.INCORRECT_METHOD_SIGNATURE,
    UnityHighlightingGroupIds.Unity,
    "Incorrect return type",
    "Incorrect return type for expected method signature.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class InvalidReturnTypeWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.InvalidReturnType";
        public const string MESSAGE = "Incorrect return type. Expected '{0}'";

        public InvalidReturnTypeWarning(IMethodDeclaration methodDeclaration, MethodSignature methodSignature)
        {
            MethodDeclaration = methodDeclaration;
            MethodSignature = methodSignature;
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

        public string ToolTip => string.Format(MESSAGE, MethodSignature.ReturnType.GetPresentableName(MethodDeclaration.Language));
        public string ErrorStripeToolTip => ToolTip;

        public IMethodDeclaration MethodDeclaration { get; }
        public MethodSignature MethodSignature { get; }
    }
}