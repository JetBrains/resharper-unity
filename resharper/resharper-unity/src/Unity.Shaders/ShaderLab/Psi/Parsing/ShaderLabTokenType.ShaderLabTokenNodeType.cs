using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing
{
    public interface IShaderLabTokenNodeType : ITokenNodeType
    {
        bool IsCommandKeyword(CachingLexer lexer); // check if this is ShaderLab command keyword
    }

    public static partial class ShaderLabTokenType
    {
        public abstract class ShaderLabTokenNodeType : TokenNodeType, IShaderLabTokenNodeType
        {
            protected ShaderLabTokenNodeType(string s, int index)
                : base(s, index)
            {
            }

            public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
            {
                throw new InvalidOperationException($"TokenNodeType.Create needs to be overridden in {GetType()}");
            }

            public override bool IsWhitespace => false;     // this == WHITESPACE || this == NEW_LINE;
            public override bool IsComment => false;
            public override bool IsStringLiteral => false;  // this == STRING_LITERAL
            public override bool IsConstantLiteral => false;    // LITERALS[this]
            public override bool IsIdentifier => false;  // this == IDENTIFIER
            public override bool IsKeyword => false;    // KEYWORDS[this]
            public virtual bool IsCommandKeyword(CachingLexer cachingLexer) => false;
        }
    }
}