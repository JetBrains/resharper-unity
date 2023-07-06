#nullable enable
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Common.Services.Tree
{
    public sealed class CLikeSyntax : SyntaxBase
    {
        public TokenNodeType? LBRACE { get; init; }
        public TokenNodeType? RBRACE { get; init; }
        public TokenNodeType? LBRACKET { get; init; }
        public TokenNodeType? RBRACKET { get; init; }
        public TokenNodeType? LPARENTH { get; init; }
        public TokenNodeType? RPARENTH { get; init; }
        public TokenNodeType? END_OF_LINE_COMMENT { get; init; }
        public TokenNodeType? C_STYLE_COMMENT { get; init; }
        public TokenNodeType? PLUS { get; init; }
        public TokenNodeType? SEMICOLON { get; init; }
        public TokenNodeType? DOT { get; init; }
        public NodeTypeSet STRING_LITERALS { get; init; } = NodeTypeSet.Empty;
        public NodeTypeSet ACCESS_CHAIN_TOKENS { get; init; } = NodeTypeSet.Empty;
    }
}