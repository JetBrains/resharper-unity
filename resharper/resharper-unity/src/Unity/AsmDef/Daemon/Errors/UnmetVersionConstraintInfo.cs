using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        Languages = "CSHARP",
        AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
        OverlapResolve = OverlapResolveKind.DEADCODE
    )]
    public class UnmetVersionConstraintInfo : IHighlighting
    {
        private readonly IJsonNewLiteralExpression myDefineValue;
        private readonly DocumentRange myHighlightingRange;

        public UnmetVersionConstraintInfo(IJsonNewLiteralExpression defineValue, string expression)
        {
            myDefineValue = defineValue;
            myHighlightingRange = defineValue.GetUnquotedDocumentRange();
            ToolTip = string.Format(Strings.UnmetVersionConstraintInfo_Symbol_not_defined__Unmet_version_constraint___0_, expression);
        }

        public bool IsValid() => myDefineValue.IsValid() && myHighlightingRange.IsValid();
        public DocumentRange CalculateRange() => myHighlightingRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}