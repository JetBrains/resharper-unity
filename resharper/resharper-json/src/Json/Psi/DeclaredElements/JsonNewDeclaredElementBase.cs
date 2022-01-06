using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements
{
    public abstract class JsonNewDeclaredElementBase : IDeclaredElement, IJsonSearchDomainOwner
    {
        private readonly IPsiServices myPsiServices;

        protected JsonNewDeclaredElementBase(IPsiServices psiServices)
        {
            myPsiServices = psiServices;
        }

        public abstract IList<IDeclaration> GetDeclarations();
        public abstract IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile);
        public abstract DeclaredElementType GetElementType();
        public abstract string ShortName { get; }

        public bool CaseSensitiveName => true;

        public virtual PsiLanguageType PresentationLanguage => JsonNewLanguage.Instance;

        public IPsiServices GetPsiServices()
        {
            return myPsiServices;
        }

        public virtual XmlNode GetXMLDoc(bool inherit)
        {
            return null;
        }

        public virtual XmlNode GetXMLDescriptionSummary(bool inherit)
        {
            return null;
        }

        public virtual bool IsValid()
        {
            return true;
        }

        public bool IsSynthetic()
        {
            return false;
        }

        public HybridCollection<IPsiSourceFile> GetSourceFiles()
        {
            return new HybridCollection<IPsiSourceFile>(GetDeclarations().Select(x => x.GetSourceFile())
                .Where(x => x != null).Distinct().ToList());
        }

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
        {
            return GetSourceFiles().Contains(sourceFile);
        }

        public abstract ISearchDomain GetSearchDomain(SearchDomainFactory factory);
    }
}