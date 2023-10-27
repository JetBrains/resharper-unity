#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.ENABLED_SHADER_KEYWORD)]
public class EnabledShaderKeywordHighlight : IHighlighting
{
    private readonly MacroReference myShaderKeywordReference;

    public EnabledShaderKeywordHighlight(MacroReference shaderKeywordReference)
    {
        myShaderKeywordReference = shaderKeywordReference;
    }
    
    public /*Localized*/ string? ToolTip => null;
    public /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myShaderKeywordReference.IsValid();

    public DocumentRange CalculateRange() => myShaderKeywordReference.GetHighlightingRange();
}