#nullable enable

using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    /// <summary>Specialization of <see cref="ErrorElement"/> with additional info (if available) about unexpected token type and expected types set for code completion.</summary>
    public class UnexpectedTokenErrorElement : ErrorElement
    {
        public TokenNodeType? TokenType { get; }
        public INodeTypeMatcher? ExpectedTokenTypes { get; }
        
        public UnexpectedTokenErrorElement(string errorDescription) : base(errorDescription)
        {
        }
        
        public UnexpectedTokenErrorElement(TokenNodeType? tokenType, INodeTypeMatcher? expectedTokenTypes, string errorDescription) : base(errorDescription)
        {
            TokenType = tokenType;
            ExpectedTokenTypes = expectedTokenTypes;
        }

        public override string ToString() => $"ErrorElement:{ErrorDescription}";
    }
}