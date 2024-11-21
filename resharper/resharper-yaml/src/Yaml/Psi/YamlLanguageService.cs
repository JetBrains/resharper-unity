using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [Language(typeof(YamlLanguage))]
  public class YamlLanguageService([NotNull] PsiLanguageType psiLanguageType, [NotNull] ILazy<IConstantValueService> constantValueService)
    : LanguageService(psiLanguageType, constantValueService)
  {
    public override ILexerFactory GetPrimaryLexerFactory()
    {
      return new YamlLexerFactory();
    }

    public override ILexer CreateFilteringLexer(ILexer lexer)
    {
      return new YamlFilteringLexer(lexer);
    }

    public override IParser CreateParser(ILexer lexer, IPsiModule module, IPsiSourceFile sourceFile)
    {
      return new YamlParser(lexer as ILexer<int> ?? lexer.ToCachingLexer());
    }

    public override IEnumerable<ITypeDeclaration> FindTypeDeclarations(IFile file)
    {
      return EmptyList<ITypeDeclaration>.Enumerable;
    }

    public override ILanguageCacheProvider CacheProvider => null;
    public override bool IsCaseSensitive => false;
    public override bool SupportTypeMemberCache => false;
    public override ITypePresenter TypePresenter => DefaultTypePresenter.Instance;
  }
}