using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        Languages = "CSHARP",
        AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
        OverlapResolve = OverlapResolveKind.DEADCODE,
        ToolTipFormatString = MESSAGE)]
    public class UnmetVersionConstraintInfo : IHighlighting
    {
        private const string MESSAGE = "Symbol not defined. Unmet version constraint: {0}";

        private readonly IJsonNewLiteralExpression myDefineValue;
        private readonly DocumentRange myHighlightingRange;

        public UnmetVersionConstraintInfo(IJsonNewLiteralExpression defineValue, string expression)
        {
            myDefineValue = defineValue;
            myHighlightingRange = defineValue.GetUnquotedDocumentRange();
            ToolTip = string.Format(MESSAGE, expression);
        }

        public bool IsValid() => myDefineValue.IsValid() && myHighlightingRange.IsValid();
        public DocumentRange CalculateRange() => myHighlightingRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}