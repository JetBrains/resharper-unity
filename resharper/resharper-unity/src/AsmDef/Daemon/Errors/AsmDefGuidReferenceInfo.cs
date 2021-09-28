using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        Languages = "CSHARP",
        AttributeId = AsmDefHighlightingAttributeIds.GUID_REFERENCE_TOOLTIP,
        OverlapResolve = OverlapResolveKind.NONE)]
    public class AsmDefGuidReferenceInfo : IHighlighting
    {
        private readonly IJsonNewLiteralExpression myLiteralExpression;

        public AsmDefGuidReferenceInfo(IJsonNewLiteralExpression literalExpression, string referenceName)
        {
            myLiteralExpression = literalExpression;
            ToolTip = referenceName;
        }

        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
        public DocumentRange CalculateRange() => myLiteralExpression.GetHighlightingRange();
        public bool IsValid() => myLiteralExpression == null || myLiteralExpression.IsValid();
    }
}