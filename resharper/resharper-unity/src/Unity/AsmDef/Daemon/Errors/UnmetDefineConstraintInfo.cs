using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        Languages = "CSHARP",
        AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
        OverlapResolve = OverlapResolveKind.DEADCODE,
        ToolTipFormatString = MESSAGE)]
    public class UnmetDefineConstraintInfo : IHighlighting
    {
        private const string MESSAGE = "Unmet define constraint{0}";
        private const string NOT_COMPILED = ". Assembly definition will not be compiled";

        private readonly IJsonNewLiteralExpression myConstraintValue;
        private readonly DocumentRange myHighlightingRange;

        public UnmetDefineConstraintInfo(IJsonNewLiteralExpression constraintValue,
                                         DocumentRange highlightingRange,
                                         bool allSymbolsUndefined)
        {
            myConstraintValue = constraintValue;
            myHighlightingRange = highlightingRange;
            ToolTip = string.Format(MESSAGE, allSymbolsUndefined ? NOT_COMPILED : string.Empty);
        }

        public bool IsValid() => myConstraintValue.IsValid() && myHighlightingRange.IsValid();
        public DocumentRange CalculateRange() => myHighlightingRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}