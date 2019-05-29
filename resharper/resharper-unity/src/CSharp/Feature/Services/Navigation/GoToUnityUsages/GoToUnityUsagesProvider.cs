using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.ExecutionHosting;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Tree;
using JetBrains.ReSharper.Features.Navigation.Features.Usages;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [ContextNavigationProvider]
    public class GoToUnityUsagesProvider : UsagesContextSearchProviderBase<UnityUsagesContextSearch>,
        INavigateFromHereProvider
    {
        internal static readonly OccurrencePresentationOptions GotoUsagePresentationOptions =
            new OccurrencePresentationOptions
            {
                IconDisplayStyle = IconDisplayStyle.OccurrenceKind,
                TextDisplayStyle = TextDisplayStyle.IdentifierAndContext
            };

        public GoToUnityUsagesProvider(IFeaturePartsContainer manager)
            : base(manager)
        {
        }

        
        public IEnumerable<ContextNavigation> CreateWorkflow(IDataContext dataContext)
        {
            var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
            var navigationExecutionHost = DefaultNavigationExecutionHost.GetInstance(solution);

            var execution = GetSearchesExecution(dataContext, navigationExecutionHost);
            if (execution != null)
            {
                yield return new ContextNavigation("U&nity Usages of Symbol", "FindUnityUsages",
                    NavigationActionGroup.Important, execution);
            }
        }

        protected override void ExecuteSearchRequest(IDataContext context, SearchRequest searchRequest, INavigationExecutionHost host)
        {
            var provider = searchRequest.Solution.GetComponent<FindUsagesAsyncViewProviderBase>();
            var viewFactory = provider.GetFactoryGotoUsages(searchRequest, host, this);
            var executer = new SearchRequestExecuter(context, searchRequest, this, host, viewFactory);
            executer.Execute();
        }
    }
}