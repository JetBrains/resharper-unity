using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [ProjectFileType(typeof(YamlProjectFileType))]
  public class YamlProjectFileLanguageService : ProjectFileLanguageService
  {
    public YamlProjectFileLanguageService()
      : base(YamlProjectFileType.Instance)
    {
    }

    public override ILexerFactory? GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile? sourceFile = null)
    {
      var languageService = YamlLanguage.Instance.LanguageService();
      return languageService?.GetPrimaryLexerFactory();
    }

    protected override PsiLanguageType PsiLanguageType =>
      (PsiLanguageType?)YamlLanguage.Instance ?? UnknownLanguage.Instance!;

    public override IconId? Icon => YamlFileTypeThemedIcons.FileYaml.Id;
  }
}