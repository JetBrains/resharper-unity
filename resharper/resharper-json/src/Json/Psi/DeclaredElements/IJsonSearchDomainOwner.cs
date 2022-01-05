using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Json.Psi.DeclaredElements
{
    public interface IJsonSearchDomainOwner
    {
        ISearchDomain GetSearchDomain(SearchDomainFactory factory);
    }
}