using JetBrains.ReSharper.Feature.Services.Cpp.TypingAssist;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Feature.Services.TypingAssists
{
  public class InjectedHlslBraceHandler : CppBraceHandler<ShaderLabLanguage>
  {
    private readonly CppDummyFormatterBase myCppDummyFormatter;

    public InjectedHlslBraceHandler(TypingAssistLanguageBase<ShaderLabLanguage> owner, CppDummyFormatterBase cppDummyFormatter) : base(owner, cppDummyFormatter)
    {
      myCppDummyFormatter = cppDummyFormatter;
    }

    protected override string CalculateBaseIndent(CppDummyFormatterContext context, CachingLexer lexer)
    {
        return myCppDummyFormatter.CalculateInjectionIndent(context, lexer);
    }

  }
}