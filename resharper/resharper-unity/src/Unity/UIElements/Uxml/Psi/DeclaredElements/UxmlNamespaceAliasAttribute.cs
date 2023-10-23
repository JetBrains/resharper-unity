using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

// ReSharper disable CheckNamespace
namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Tree
{
    public partial class UxmlNamespaceAliasAttribute : IDeclaredElement
    {
        public string ShortName => DeclaredName;
        public bool CaseSensitiveName => true;

        public PsiLanguageType PresentationLanguage => UxmlLanguage.Instance!;

        public DeclaredElementType GetElementType()
        {
            return UxmlDeclaredElementType.NamespaceAlias;
        }

        public XmlNode GetXMLDescriptionSummary(bool inherit)
        {
            return null;
        }

        public IList<IDeclaration> GetDeclarations()
        {
            return new IDeclaration[] { this };
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return SharedImplUtil.GetDeclarationsIn(this, sourceFile);
        }
    }
}