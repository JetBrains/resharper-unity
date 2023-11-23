#nullable enable
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport;

public static class HlslUtils
{
    private static readonly NodeTypeSet ourConditionalDirectiveTokens = new(CppTokenNodeTypes.IFDEF_DIRECTIVE, CppTokenNodeTypes.IFNDEF_DIRECTIVE, CppTokenNodeTypes.IF_DIRECTIVE, CppTokenNodeTypes.ELIF_DIRECTIVE);
    
    public static bool IsConditionalDirective(Directive directive) => ourConditionalDirectiveTokens[directive.Head.NodeType];
}