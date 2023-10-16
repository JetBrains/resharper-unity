#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Cpp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderVariantsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.SHADER_VARIANT)]
public class ShaderVariantHighlight : IHighlighting
{
    private readonly MacroReference myShaderVariantReference;

    public ShaderVariantHighlight(MacroReference shaderVariantReference)
    {
        myShaderVariantReference = shaderVariantReference;
    }
    
    public /*Localized*/ string? ToolTip => null;
    public /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myShaderVariantReference.IsValid();

    public DocumentRange CalculateRange() => myShaderVariantReference.GetHighlightingRange();
    
    [RegisterStaticHighlightingsGroup("Shader Variants Highlighting", IsVisible: true)]
    public static class ShaderVariantsHighlightingId
    {
    }
}