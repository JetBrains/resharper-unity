#nullable enable
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.ENABLED_SHADER_KEYWORD)]
public class EnabledShaderKeywordHighlight : ShaderKeywordHighlight
{
    public EnabledShaderKeywordHighlight(string keyword, ITreeNode shaderKeywordNode) : base(keyword, shaderKeywordNode) { }
}