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
        private class CommandKeywordTokenNodeType(string s, int index, string representation) : KeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer lexer) => ShaderLabKeywordType.RegularCommand;
            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => ShaderLabKeywordType.RegularCommand;
        }
        
        /// <summary>Sub-class of keyword token types for ShaderLab block command keywords.</summary>
        private class BlockCommandKeywordTokenNodeType(string s, int index, string representation) : CommandKeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer lexer) => ShaderLabKeywordType.BlockCommand;
            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => ShaderLabKeywordType.BlockCommand;
        }
        
        private class CommandArgumentKeywordTokenNodeType(string s, int index, string representation) : CommandKeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer lexer) => ShaderLabKeywordType.CommandArgument;
            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => ShaderLabKeywordType.CommandArgument;
        }

        private class PropertyTypeTokenNodeType(string s, int index, string representation) : KeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer lexer) => ShaderLabKeywordType.PropertyType;
            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => ShaderLabKeywordType.PropertyType;
        }

        private class TextureDimensionTokenNodeType(string s, int index, string representation) : KeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer cachingLexer)
            {
                Assertion.Assert(cachingLexer.TokenType == this);
                return GetPreviousTokenType(cachingLexer) == COMMA ? ShaderLabKeywordType.PropertyType : ShaderLabKeywordType.Unknown;
            }

            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => placement.Parent is IPropertyType or IPropertyDeclaration ? ShaderLabKeywordType.PropertyType : ShaderLabKeywordType.Unknown;
        }
        
        private class PropertyAndCommandKeywordTokenNodeType(string s, int index, string representation) : KeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer cachingLexer)
            {
                Assertion.Assert(cachingLexer.TokenType == this);
                return GetPreviousTokenType(cachingLexer) == COMMA ? ShaderLabKeywordType.PropertyType : ShaderLabKeywordType.RegularCommand;
            }

            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => placement.Parent is IPropertyType or IPropertyDeclaration ? ShaderLabKeywordType.PropertyType : ShaderLabKeywordType.RegularCommand;
        }
        
        private class EmissionCommandKeywordTokenNodeType(string s, int index, string representation) : KeywordTokenNodeType(s, index, representation)
        {
            public override ShaderLabKeywordType GetKeywordType(CachingLexer cachingLexer)
            {
                Assertion.Assert(cachingLexer.TokenType == this);
                return GetPreviousTokenType(cachingLexer) == COLOR_MATERIAL_KEYWORD ? ShaderLabKeywordType.Unknown : ShaderLabKeywordType.RegularCommand;
            }

            public override ShaderLabKeywordType GetKeywordType(ITreeNode placement) => placement.Parent is IColorMaterialCommand ? ShaderLabKeywordType.Unknown : ShaderLabKeywordType.RegularCommand;
        }
    }
}
