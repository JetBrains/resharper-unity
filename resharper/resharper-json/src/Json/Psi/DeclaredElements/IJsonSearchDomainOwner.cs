using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.DeclaredElements
{
    public interface IJsonSearchDomainOwner
    {
        ISearchDomain GetSearchDomain(SearchDomainFactory factory);
    }
}