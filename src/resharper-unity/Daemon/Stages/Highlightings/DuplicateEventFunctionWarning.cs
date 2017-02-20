using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

[assembly: RegisterConfigurableSeverity(DuplicateEventFunctionWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, DuplicateEventFunctionWarning.MESSAGE,
    "Event function with the same name is already declared. Unity will use one of the functions, but the usage is ambiguous.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    // TODO: Check out CSharpConflictDeclarationsContextSearch
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.WARNING,
        ToolTipFormatString = MESSAGE)]
    public class DuplicateEventFunctionWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.DuplicateEventFunction";
        public const string MESSAGE = "Event function with the same name is already declared";

        private readonly IMethodDeclaration myMethodDeclaration;

        public DuplicateEventFunctionWarning(IMethodDeclaration methodDeclaration)
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

            var rpar = myMethodDeclaration.RPar;
            if (rpar == null)
                return nameRange;

            var rparRange = rpar.GetDocumentRange();
            if (!rparRange.IsValid() || nameRange.Document != rparRange.Document)
                return nameRange;

            return new DocumentRange(nameRange.Document, new TextRange(nameRange.TextRange.StartOffset, rparRange.TextRange.EndOffset));
        }

        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}