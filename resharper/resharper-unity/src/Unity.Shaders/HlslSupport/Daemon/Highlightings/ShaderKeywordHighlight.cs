#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;

public abstract class ShaderKeywordHighlight : IHighlighting
{
    private readonly ITreeNode myShaderKeywordNode;

    protected ShaderKeywordHighlight(string keyword, ITreeNode shaderKeywordNode)
    {
        Keyword = keyword;
        myShaderKeywordNode = shaderKeywordNode;
    }
    
    public string Keyword { get; }

    public virtual /*Localized*/ string? ToolTip => null;
    public virtual /*Localized*/ string? ErrorStripeToolTip => null;
    public bool IsValid() => myShaderKeywordNode.IsValid();

    public DocumentRange CalculateRange() => myShaderKeywordNode.GetHighlightingRange();
}