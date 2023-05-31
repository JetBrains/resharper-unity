#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing
{
    public partial class ShaderLabTokenType
    {
        private static TokenNodeType? GetPreviousTokenType(CachingLexer cachingLexer)
        {
            var savedPosition = cachingLexer.CurrentPosition;
            cachingLexer.Advance(-1);
            while (cachingLexer.TokenType is { IsWhitespace: true })
                cachingLexer.Advance(-1);
            var tokenType = cachingLexer.TokenType;
            cachingLexer.CurrentPosition = savedPosition;
            return tokenType;
        }
        
        /// <summary>Sub-class of keyword token types for ShaderLab command keywords.</summary>
        private class CommandKeywordTokenNodeType : KeywordTokenNodeType
        {
            public CommandKeywordTokenNodeType(string s, int index, string representation)
                : base(s, index, representation)
            {
            }

            public override bool IsCommandKeyword(CachingLexer lexer) => true;
            public override bool IsCommandKeyword(ITreeNode placement) => true;
        }
        
        private class PropertyAndCommandKeywordTokenNodeType : KeywordTokenNodeType
        {
            public PropertyAndCommandKeywordTokenNodeType(string s, int index, string representation) : base(s, index, representation)
            {
            }

            public override bool IsCommandKeyword(CachingLexer cachingLexer)
            {
                Assertion.Assert(cachingLexer.TokenType == this);
                return GetPreviousTokenType(cachingLexer) != COMMA;
            }

            public override bool IsCommandKeyword(ITreeNode placement) => placement.Parent is not IPropertyDeclaration;
        }
        
        private class EmissionCommandKeywordTokenNodeType : KeywordTokenNodeType
        {
            public EmissionCommandKeywordTokenNodeType(string s, int index, string representation) : base(s, index, representation)
            {
            }
            
            public override bool IsCommandKeyword(CachingLexer cachingLexer)
            {
                Assertion.Assert(cachingLexer.TokenType == this);
                return GetPreviousTokenType(cachingLexer) != COLOR_MATERIAL_KEYWORD;
            }

            public override bool IsCommandKeyword(ITreeNode placement) => placement.Parent is not IColorMaterialCommand;
        }
    }
}
