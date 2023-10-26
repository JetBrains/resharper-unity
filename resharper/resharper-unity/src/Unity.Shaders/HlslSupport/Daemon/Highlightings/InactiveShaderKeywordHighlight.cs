#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Cpp.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.INACTIVE_SHADER_KEYWORD)]
public class InactiveShaderKeywordHighlight : IHighlighting
{
    private readonly CppIdentifierTokenNode myIdentifier;

    public InactiveShaderKeywordHighlight(CppIdentifierTokenNode identifier)
    {
        myIdentifier = identifier;
    }
    
    public /*Localized*/ string? ToolTip => null;
    public /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myIdentifier.IsValid();

    public DocumentRange CalculateRange() => myIdentifier.GetHighlightingRange();
}