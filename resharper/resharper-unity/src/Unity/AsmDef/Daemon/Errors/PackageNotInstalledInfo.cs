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
        OverlapResolve = OverlapResolveKind.DEADCODE,
        ToolTipFormatStringResourceType = typeof(Strings),
        ToolTipFormatStringResourceName = nameof(Strings.PackageNotInstalledInfo_Symbol_not_defined__Package___0___is_not_installed))]
    public class PackageNotInstalledInfo : IHighlighting
    {
        private readonly IJsonNewLiteralExpression myDefineValue;
        private readonly DocumentRange myHighlightingRange;

        public PackageNotInstalledInfo(IJsonNewLiteralExpression defineValue, string packageId)
        {
            myDefineValue = defineValue;
            myHighlightingRange = defineValue.GetUnquotedDocumentRange();
            ToolTip = string.Format(Strings.PackageNotInstalledInfo_Symbol_not_defined__Package___0___is_not_installed, packageId);
        }

        public bool IsValid() => myDefineValue.IsValid() && myHighlightingRange.IsValid();
        public DocumentRange CalculateRange() => myHighlightingRange;
        public string ToolTip { get; }
        public string ErrorStripeToolTip => ToolTip;
    }
}