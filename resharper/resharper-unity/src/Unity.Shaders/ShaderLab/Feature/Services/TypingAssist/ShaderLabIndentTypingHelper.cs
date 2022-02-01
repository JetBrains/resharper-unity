using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.Cpp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.TypingAssist
{
    public class ShaderLabIndentTypingHelper : IndentTypingHelper<ShaderLabLanguage>
    {
        public ShaderLabIndentTypingHelper(TypingAssistLanguageBase<ShaderLabLanguage> assist)
            : base(assist)
        {
        }
        
        // smart backspaces expecteed that GetExtraStub return not null value, "foo " is typical value
        protected override string GetExtraStub(CachingLexer lexer, int offset, ITextControl textControl)
        {
            using (LexerStateCookie.Create(lexer))
            {
                lexer.FindTokenAt(offset);
                if (!(lexer.TokenType is CppTokenNodeType))
                    return "foo ";
            }
            return base.GetExtraStub(lexer, offset, textControl);
        }
    }
}