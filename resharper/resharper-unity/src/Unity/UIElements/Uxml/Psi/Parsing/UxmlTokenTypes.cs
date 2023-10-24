using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Xml;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Parsing
{
    [Language(typeof(UxmlLanguage))]
    public class UxmlTokenTypes : XmlTokenTypesImpl
    {
        public new static UxmlTokenTypes GetInstance(PsiLanguageType languageType)
        {
            return LanguageManager.Instance.GetService<UxmlTokenTypes>(languageType);
        }

        public new static UxmlTokenTypes GetInstance<TLanguage>()
            where TLanguage : PsiLanguageType
        {
            return LanguageManager.Instance.GetService<UxmlTokenTypes, TLanguage>();
        }

        public UxmlTokenTypes(PsiLanguageType languageType, IXmlTokenBuilder xmlTokenBuilder)
            : base(languageType, xmlTokenBuilder)
        {
        }
    }
}