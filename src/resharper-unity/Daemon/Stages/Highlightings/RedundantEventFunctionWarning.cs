using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

[assembly: RegisterConfigurableSeverity(RedundantEventFunctionWarning.HIGHLIGHTING_ID,
    null, UnityHighlightingGroupIds.Unity, RedundantEventFunctionWarning.MESSAGE,
    "Empty Unity event functions are still called from native code, which affects performance.",
    Severity.WARNING)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, CSharpLanguage.Name,
        AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
        OverlapResolve = OverlapResolveKind.DEADCODE,
        ToolTipFormatString = MESSAGE)]
    public class RedundantEventFunctionWarning : IHighlighting, IUnityHighlighting
    {
        public const string HIGHLIGHTING_ID = "Unity.RedundantEventFunction";
        public const string MESSAGE = "Redundant Unity event function";

        private readonly IMethodDeclaration myDeclaration;

        public RedundantEventFunctionWarning(IMethodDeclaration declaration)
        {
            myDeclaration = declaration;
        }

        public bool IsValid() => myDeclaration == null || myDeclaration.IsValid();
        public DocumentRange CalculateRange() => myDeclaration.GetHighlightingRange();
        public string ToolTip => MESSAGE;
        public string ErrorStripeToolTip => ToolTip;
    }
}