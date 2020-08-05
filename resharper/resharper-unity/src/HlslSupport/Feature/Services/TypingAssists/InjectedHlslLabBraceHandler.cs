using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Feature.Services.TypingAssists
{
  public class InjectedHlslBraceHandler : CppBraceHandler<ShaderLabLanguage>
  {
    private readonly CppDummyFormatterBase myCppDummyFormatter;

    public InjectedHlslBraceHandler(TypingAssistLanguageBase<ShaderLabLanguage> owner, CppDummyFormatterBase cppDummyFormatter, bool isRiderMode, bool isInternalMode) : base(owner, cppDummyFormatter, isRiderMode, isInternalMode)
    {
      myCppDummyFormatter = cppDummyFormatter;
    }

    protected override string CalculateBaseIndent(CachingLexer lexer, ITextControl textControl)
    {
      return myCppDummyFormatter.CalculateInjectionIndent(lexer, textControl);
    }
  }
}