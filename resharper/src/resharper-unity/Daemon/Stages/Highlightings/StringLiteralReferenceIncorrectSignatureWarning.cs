using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Psi.Resolve;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;

[assembly: RegisterConfigurableSeverity(StringLiteralReferenceIncorrectSignatureWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity,
    "Method referenced in string literal does not have the expected signature.",
    "Method referenced in string literal does not have the expected signature.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class StringLiteralReferenceIncorrectSignatureWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.IncorrectSignature";
        public const string MESSAGE = "Expected a method with '{0} {1}({2})' signature";

        private readonly UnityEventFunctionReference myReference;

        public StringLiteralReferenceIncorrectSignatureWarning(UnityEventFunctionReference reference)
        {
            myReference = reference;
        }

        public bool IsValid()
        {
            return myReference == null || myReference.IsValid();
        }

        public DocumentRange CalculateRange() => myReference.GetDocumentRange();

        public string ToolTip
        {
            get
            {
                var methodSignature = myReference.MethodSignature;
                var returnType = methodSignature.ReturnType.GetPresentableName(CSharpLanguage.Instance);
                var parameterTypes = myReference.MethodSignature.Parameters.GetParameterTypes();
                return string.Format(MESSAGE, returnType, myReference.GetName(), parameterTypes);
            }
        }

        public string ErrorStripeToolTip => ToolTip;
    }
}