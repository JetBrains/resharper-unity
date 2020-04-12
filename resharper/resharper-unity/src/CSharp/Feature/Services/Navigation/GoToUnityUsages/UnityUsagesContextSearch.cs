using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Features.Navigation.Features.Usages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [ShellFeaturePart]
    public class UnityUsagesContextSearch : UsagesContextSearchBase
    {
        protected override ISearchDomain CreateSearchDomain(IDataContext context)
        {
            var solution = context.GetData(ProjectModelDataConstants.SOLUTION);
            Assertion.Assert(solution != null, "solution != null");
            var searchDomainFactory = solution.GetComponent<SearchDomainFactory>();
            var unityModule = solution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
            Assertion.Assert(unityModule != null, "unityModule != null");
            
            return searchDomainFactory.CreateSearchDomain(unityModule);
        }
    }
}