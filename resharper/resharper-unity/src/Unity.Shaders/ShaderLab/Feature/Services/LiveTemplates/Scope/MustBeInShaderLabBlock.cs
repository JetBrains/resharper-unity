#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Application.I18n;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Resources;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.LiveTemplates.Scope
{
    /// Mandatory scope point. Must be directly inside of ShaderLab block. Can be used for creation of HLSL blocks, Blend commands, Properties, Shader passes etc. 
    public class MustBeInShaderLabBlock : InUnityShaderLabFile, IMandatoryScopePoint
    {
        public const string BlockKeywordAttributeName = "blockKeyword";
        
        private static readonly Guid ourDefaultGuid = new("0ACEC8E2-0B11-4318-9264-9221D7E632A3");
        private static readonly Dictionary<string, TokenNodeType> ourKnownKeywords = new();

        static MustBeInShaderLabBlock()
        {
            foreach (var keyword in ShaderLabTokenType.BLOCK_COMMAND_KEYWORDS)
            {
                if (keyword is TokenNodeType tokenNodeType)
                    ourKnownKeywords[tokenNodeType.TokenRepresentation] = tokenNodeType;
            }
        }
        
        public TokenNodeType CommandKeyword { get; }

        public MustBeInShaderLabBlock(TokenNodeType commandKeyword) => CommandKeyword = commandKeyword;

        public static IEnumerable<string> KnownKeywords => ourKnownKeywords.Keys;
        
        public static MustBeInShaderLabBlock? TryCreateFromCommandKeyword(string commandKeyword) => ourKnownKeywords.TryGetValue(commandKeyword, out var tokenNodeType) ? new MustBeInShaderLabBlock(tokenNodeType) : null; 

        public override IEnumerable<Pair<string, string>> EnumerateCustomProperties() => FixedList.Of(new Pair<string, string>(BlockKeywordAttributeName, CommandKeyword.TokenRepresentation));

        public override bool IsSubsetOf(ITemplateScopePoint other) => 
            base.IsSubsetOf(other) 
            && (other is not MustBeInShaderLabBlock otherInShaderLabBlock || otherInShaderLabBlock.CommandKeyword == CommandKeyword);

        public override Guid GetDefaultUID() => ourDefaultGuid;
        public override string PresentableShortName => Strings.InUnityShaderLabBlock_PresentableShortName.Format(CommandKeyword.TokenRepresentation);
        public override string ToString() => PresentableShortName;
    }
}