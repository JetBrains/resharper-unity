using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Json.Psi.Tree;
using JetBrains.ReSharper.Plugins.Unity.Resources;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors
{
    [StaticSeverityHighlighting(Severity.INFO,
        typeof(HighlightingGroupIds.IdentifierHighlightings),
        Languages = "CSHARP",
        AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
        OverlapResolve = OverlapResolveKind.DEADCODE,
        ToolTipFormatStringResourceType = typeof(Strings),
        ToolTipFormatStringResourceName = nameof(Strings.UnmetDefineConstraintInfo_Unmet_define_constraint_0_)
        )]
    public class UnmetDefineConstraintInfo : IHighlighting
    {
        private readonly IJsonNewLiteralExpression myConstraintValue;
        private readonly DocumentRange myHighlightingRange;

        public UnmetDefineConstraintInfo(IJsonNewLiteralExpression constraintValue,
                                         DocumentRange highlightingRange,
                                         bool allSymbolsUndefined)
        {
            myConstraintValue = constraintValue;
            myHighlightingRange = highlightingRange;
            ToolTip = allSymbolsUndefined ? 
                Strings.UnmetDefineConstraintInfo_Unmet_define_constraint_Assembly_definition_will_not_be_compiled 
                : Strings.UnmetDefineConstraintInfo_Unmet_define_constraint;
        }

        public bool IsValid() => myConstraintValue.IsValid() && myHighlightingRange.IsValid();
        public DocumentRange CalculateRange() => myHighlightingRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}