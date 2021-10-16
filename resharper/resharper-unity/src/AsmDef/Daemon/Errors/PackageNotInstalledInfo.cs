using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Tree;
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
    public class PackageNotInstalledInfo : IHighlighting
    {
        private const string MESSAGE = "Symbol not defined. Package '{0}' is not installed";

        private readonly IJsonNewLiteralExpression myDefineValue;
        private readonly DocumentRange myHighlightingRange;

        public PackageNotInstalledInfo(IJsonNewLiteralExpression defineValue, string packageId)
        {
            myDefineValue = defineValue;
            myHighlightingRange = defineValue.GetUnquotedDocumentRange();
            ToolTip = string.Format(MESSAGE, packageId);
        }

        public bool IsValid() => myDefineValue.IsValid() && myHighlightingRange.IsValid();
        public DocumentRange CalculateRange() => myHighlightingRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}