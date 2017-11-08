using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

[assembly: RegisterConfigurableSeverity(InvalidTypeParametersWarning.HIGHLIGHTING_ID,
    UnityHighlightingGroupIds.INCORRECT_METHOD_SIGNATURE,
    UnityHighlightingGroupIds.Unity,
    "Incorrect type parameters",
    "Incorrect type parameters for expected method signature.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING)]
    public class InvalidTypeParametersWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.InvalidTypeParameters";

        public InvalidTypeParametersWarning(IMethodDeclaration methodDeclaration, MethodSignature methodSignature)
        {
            MethodDeclaration = methodDeclaration;
            ExpectedMethodSignature = methodSignature;
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

            var typeParameterList = MethodDeclaration.TypeParameterList;
            if (typeParameterList == null)
                return nameRange;

            var documentRange = typeParameterList.GetDocumentRange();
            return !documentRange.IsValid() ? nameRange : documentRange;
        }

        public string ToolTip => "Incorrect type parameters";
        public string ErrorStripeToolTip => ToolTip;

        public IMethodDeclaration MethodDeclaration { get; }
        public MethodSignature ExpectedMethodSignature { get; }
    }
}