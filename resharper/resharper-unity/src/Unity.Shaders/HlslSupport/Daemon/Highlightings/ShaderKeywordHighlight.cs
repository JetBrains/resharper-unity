#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.SHADER_KEYWORD)]
public class ShaderKeywordHighlight : IHighlighting
{
    private readonly MacroReference myShaderVariantReference;

    public ShaderKeywordHighlight(MacroReference shaderVariantReference)
    {
        myShaderVariantReference = shaderVariantReference;
    }
    
    public /*Localized*/ string? ToolTip => null;
    public /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myShaderVariantReference.IsValid();

    public DocumentRange CalculateRange() => myShaderVariantReference.GetHighlightingRange();
    
    [RegisterStaticHighlightingsGroup("Shader Keywords Highlighting", IsVisible: true)]
    public static class ShaderKeywordsHighlightingId
    {
    }
}