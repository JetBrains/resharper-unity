#nullable enable

using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    /// <summary>Specialization of <see cref="ErrorElement"/> with additional info (if available) about unexpected token type and expected types set for code completion.</summary>
    public class UnexpectedTokenErrorElement : ErrorElement
    {
        public TokenNodeType? TokenType { get; }
        public IEnumerable<NodeType>? ExpectedTokenTypes { get; }
        
        public UnexpectedTokenErrorElement(string errorDescription) : base(errorDescription)
        {
        }
        
        public UnexpectedTokenErrorElement(TokenNodeType? tokenType, IEnumerable<NodeType>? expectedTokenTypes, string errorDescription) : base(errorDescription)
        {
            Assertion.Assert(expectedTokenTypes is null or NodeTypeSet or NodeType[], "expectedTokenTypes should be multiple-enumerable. NodeTypeSet and array supported for now.");
            TokenType = tokenType;
            ExpectedTokenTypes = expectedTokenTypes;
        }

        public override string ToString() => $"ErrorElement:{ErrorDescription}";
    }
}