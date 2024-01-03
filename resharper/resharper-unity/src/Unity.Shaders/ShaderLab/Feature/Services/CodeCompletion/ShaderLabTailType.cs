#nullable enable
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.CodeCompletion
{
    public class ShaderLabTailType : TailType
    {
        private readonly TokenNodeType[] myTokens;
        
        // ' {caret}'
        public static readonly TailType Space = new ShaderLabTailType(nameof(Space),
            ShaderLabTokenType.WHITESPACE, CaretTokenNodeType.Instance) { SkipTypings = new[] { " " } };
        
        // '{{caret}}
        public static readonly TailType Braces = new ShaderLabTailType(nameof(Braces),
            ShaderLabTokenType.LBRACE, CaretTokenNodeType.Instance, ShaderLabTokenType.RBRACE) { SkipTypings = new[] { "{" } };
        
        // '[{caret}]
        public static readonly TailType Brackets = new ShaderLabTailType(nameof(Brackets),
            ShaderLabTokenType.LBRACK, CaretTokenNodeType.Instance, ShaderLabTokenType.RBRACK) { SkipTypings = new[] { "[" } };
        
        private ShaderLabTailType(string name, params TokenNodeType[] tokens) : base(name)
        {
            myTokens = tokens;
        }
        
        public override TokenNodeType[] EvaluateTail(ISolution solution, IDocument document) => myTokens;
    }
}