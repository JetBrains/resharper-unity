#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

public abstract class ShaderKeywordHighlight(string keyword, ITreeNode shaderKeywordNode) : IHighlighting
{
    public string Keyword { get; } = keyword;

    public virtual /*Localized*/ string? ToolTip => null;
    public virtual /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => shaderKeywordNode.IsValid();

    public DocumentRange CalculateRange() => shaderKeywordNode.GetHighlightingRange();
    public bool IsInUrtShader() => UnityShaderFileUtils.IsUrtShaderFile(shaderKeywordNode.GetSourceFile().GetLocation());
}