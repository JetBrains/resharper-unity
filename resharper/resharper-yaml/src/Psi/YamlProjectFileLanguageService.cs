using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Resources;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi
{
  [ProjectFileType(typeof(YamlProjectFileType))]
  public class YamlProjectFileLanguageService : ProjectFileLanguageService
  {
    public YamlProjectFileLanguageService(ProjectFileType projectFileType) : base(projectFileType)
    {
    }

    public override ILexerFactory GetMixedLexerFactory(ISolution solution, IBuffer buffer, IPsiSourceFile sourceFile = null)
    {
      var languageService = YamlLanguage.Instance.LanguageService();
      return languageService?.GetPrimaryLexerFactory();
    }

    protected override PsiLanguageType PsiLanguageType => (PsiLanguageType) YamlLanguage.Instance ?? UnknownLanguage.Instance;

    // TODO: Proper icon!
    public override IconId Icon => PsiCSharpThemedIcons.Csharp.Id;
  }
}