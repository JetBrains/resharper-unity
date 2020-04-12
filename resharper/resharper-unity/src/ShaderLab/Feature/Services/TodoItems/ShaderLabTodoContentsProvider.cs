using JetBrains.ReSharper.Feature.Services.TodoItems;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Feature.Services.TodoItems
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabTodoContentsProvider : DefaultTodoContentsProvider
    {
        public override TextRange? GetTokenContentsRange(string documentText, TextRange tokenRange, TokenNodeType tokenType)
        {
            if (tokenType == ShaderLabTokenType.END_OF_LINE_COMMENT)
            {
                return new TextRange(tokenRange.StartOffset + 2, tokenRange.EndOffset);
            }
            if (tokenType == ShaderLabTokenType.MULTI_LINE_COMMENT)
            {
                return new TextRange(tokenRange.StartOffset + 2, tokenRange.EndOffset - 2);
            }

            return base.GetTokenContentsRange(documentText, tokenRange, tokenType);
        }
    }
}