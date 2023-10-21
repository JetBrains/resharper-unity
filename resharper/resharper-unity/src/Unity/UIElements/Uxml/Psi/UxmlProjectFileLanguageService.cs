using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi
{
    [ProjectFileType(typeof(UxmlProjectFileType))]
    public class UxmlProjectFileLanguageService : XmlProjectFileLanguageService
    {
        public UxmlProjectFileLanguageService(UxmlProjectFileType projectFileType)
            : base(projectFileType)
        {
        }

        public override PsiLanguageType GetPsiLanguageType(ProjectFileType languageType)
        {
            this.AssertProjectFileType(languageType);
            return UxmlLanguage.Instance!;
        }
    }
}