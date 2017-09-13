using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;

// TODO: Where should this live?
// Also, where does it get used?
[assembly: RegisterStaticHighlightingsGroup("ShaderLabErrors", "ShaderLab Errors", true)]
[assembly: RegisterStaticHighlightingsGroup("ShaderLabWarnings", "ShaderLab Warnings", true)]

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Daemon.Stages.Highlightings
{
    [StaticSeverityHighlighting(Severity.ERROR, "ShaderLabErrors", Languages = "SHADERLAB",
        AttributeId = HighlightingAttributeIds.ERROR_ATTRIBUTE, OverlapResolve = OverlapResolveKind.ERROR)]
    public class ShaderLabSyntaxError : SyntaxErrorBase
    {
        public ShaderLabSyntaxError(string toolTip, DocumentRange range)
            : base(toolTip, range)
        {
        }
    }
}