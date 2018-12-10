using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Resources.Icons;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [ProjectFileType(typeof(YamlProjectFileType))]
  public class YamlProjectFileLanguageService : ProjectFileLanguageService
  {
    private readonly YamlSupport myYamlSupport;

    public YamlProjectFileLanguageService(YamlSupport yamlSupport)
      : base(YamlProjectFileType.Instance)
    {
      myYamlSupport = yamlSupport;
    }

    public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
    {
      var languageService = YamlLanguage.Instance.LanguageService();
      return languageService?.GetPrimaryLexerFactory();
    }

    protected override PsiLanguageType PsiLanguageType
    {
      get
      {
        var yamlLanguage = (PsiLanguageType) YamlLanguage.Instance ?? UnknownLanguage.Instance;
        return myYamlSupport.IsParsingEnabled.Value ? yamlLanguage : UnknownLanguage.Instance;
      }
    }

    public override IconId Icon => YamlFileTypeThemedIcons.FileYaml.Id;
  }
}