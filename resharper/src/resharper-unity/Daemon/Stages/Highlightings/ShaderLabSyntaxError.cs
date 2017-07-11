using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Impl;
using JetBrains.ReSharper.Feature.Services.Daemon;

// TODO: Where should this live?
[assembly: RegisterStaticHighlightingsGroup("ShaderLabErrors", "ShaderLab Errors", true)]

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings
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