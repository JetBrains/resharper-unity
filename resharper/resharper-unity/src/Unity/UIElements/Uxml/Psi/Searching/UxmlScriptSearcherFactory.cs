using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.Unity.UIElements.Uxml.Psi.Searching
{
  [PsiComponent]
  public class UxmlScriptSearcherFactory : DomainSpecificSearcherFactoryBase
  { 
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
    {
      return languageType.Is<UxmlLanguage>();
    }

    public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
    {
      yield return element.ShortName;
    }

    public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
      ReferenceSearcherParameters referenceSearcherParameters)
    {
      return new UxmlDomainSpecificSearcher(this, elements, referenceSearcherParameters);
    }
  }
}