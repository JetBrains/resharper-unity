#nullable enable
using System.Collections.Immutable;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Daemon.Stages;
using JetBrains.ReSharper.Psi.Cpp.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

[StaticSeverityHighlighting(Severity.INFO, typeof(ShaderKeywordsHighlightingId), OverlapResolve = OverlapResolveKind.NONE, AttributeId = ShaderLabHighlightingAttributeIds.SUPPRESSED_SHADER_KEYWORD)]
public class SuppressedShaderKeywordHighlight : ShaderKeywordHighlight
{
    public string? SuppressorsString { get; }
    
    public ImmutableArray<string> Suppressors { get; }

    public SuppressedShaderKeywordHighlight(string keyword, CppIdentifierTokenNode shaderKeywordNode, ImmutableArray<string> suppressors) : base(keyword, shaderKeywordNode)
    {
        Suppressors = suppressors;
        SuppressorsString = !suppressors.IsEmpty ? string.Join(", ", suppressors) : null;
    }
    
    public override /*Localized*/ string? ToolTip => SuppressorsString != null ? $"Suppressed because of another enabled keywords in the same shader keyword set: {SuppressorsString}.\n\nCheck multi_compile/shader_feature pragmas for conflicts." : null;
}