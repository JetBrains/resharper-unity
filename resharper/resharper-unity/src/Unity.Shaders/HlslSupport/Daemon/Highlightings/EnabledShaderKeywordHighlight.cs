#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.ENABLED_SHADER_KEYWORD)]
public class EnabledShaderKeywordHighlight : IHighlighting
{
    private readonly ITreeNode myShaderKeywordNode;

    public EnabledShaderKeywordHighlight(ITreeNode shaderKeywordNode)
    {
        myShaderKeywordNode = shaderKeywordNode;
    }
    
    public /*Localized*/ string? ToolTip => null;
    public /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myShaderKeywordNode.IsValid();

    public DocumentRange CalculateRange() => myShaderKeywordNode.GetHighlightingRange();
}