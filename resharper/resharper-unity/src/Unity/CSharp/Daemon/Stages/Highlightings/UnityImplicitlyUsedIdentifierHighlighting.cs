using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.CSharp;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        AttributeId = UnityHighlightingAttributeIds.UNITY_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE,
        Languages = CSharpLanguage.Name,
        OverlapResolve = OverlapResolveKind.NONE)]
    public class UnityImplicitlyUsedIdentifierHighlighting : IHighlighting, IUnityHighlighting
    {
        private readonly DocumentRange myDocumentRange;

        public UnityImplicitlyUsedIdentifierHighlighting(DocumentRange documentRange)
        {
            myDocumentRange = documentRange;
        }

        public bool IsValid() => true;

        public DocumentRange CalculateRange() => myDocumentRange;

        public string ToolTip => null;
        public string ErrorStripeToolTip => null;
    }
}