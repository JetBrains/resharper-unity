using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Searching
{
  [PsiComponent]
  public class UxmlScriptSearcherFactory : DomainSpecificSearcherFactoryBase
  {
    private readonly SearchDomainFactory mySearchDomainFactory;
    private readonly IPsiModules myPSIModules;

    public UxmlScriptSearcherFactory(SearchDomainFactory searchDomainFactory, IPsiModules psiModules)
    {
      mySearchDomainFactory = searchDomainFactory;
      myPSIModules = psiModules;
    }
    
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
    {
      return languageType.Is<UxmlLanguage>();
    }

    public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
      ReferenceSearcherParameters referenceSearcherParameters)
    {
      return new UxmlDomainSpecificSearcher(this, elements, referenceSearcherParameters);
    }
  }
}