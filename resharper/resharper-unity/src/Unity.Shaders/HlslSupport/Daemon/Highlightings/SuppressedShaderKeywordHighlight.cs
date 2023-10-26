#nullable enable
using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Cpp.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.SUPPRESSED_SHADER_KEYWORD)]
public class SuppressedShaderKeywordHighlight : IHighlighting
{
    private readonly CppIdentifierTokenNode myIdentifier;
    private readonly string mySuppressors;

    public SuppressedShaderKeywordHighlight(CppIdentifierTokenNode identifier, List<string> suppressors)
    {
        myIdentifier = identifier;
        mySuppressors = string.Join(", ", suppressors);
    }
    
    public /*Localized*/ string ToolTip => $"Suppressed by: {mySuppressors}";
    public /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myIdentifier.IsValid();

    public DocumentRange CalculateRange() => myIdentifier.GetHighlightingRange();
}