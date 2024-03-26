using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.ActionsRevised.Loader;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.ExecutionHosting;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Features.Navigation.Features.Usages;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [ContextNavigationProvider]
    public class GoToUnityUsagesProvider : UsagesContextSearchProviderBase<UnityUsagesContextSearch>,
        INavigateFromHereProvider
    {
        public GoToUnityUsagesProvider(IFeaturePartsContainer manager)
            : base(manager)
        {
        }


        public IEnumerable<ContextNavigation> CreateWorkflow(IDataContext dataContext)
        {
            var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
            if (solution == null)
                yield break;

            var solutionTracker = solution.GetComponent<UnitySolutionTracker>();
            if (!solutionTracker.IsUnityProject.HasTrueValue())
                yield break;
            
            solution.GetComponent<UnityUsagesDeferredCachesNotification>().CheckAndShowNotification();
            
            var navigationExecutionHost = DefaultNavigationExecutionHost.GetInstance(solution);

            var execution = GetSearchesExecution(dataContext, navigationExecutionHost);
            if (execution != null)
            {
                yield return new ContextNavigation(Strings.GoToUnityUsagesProvider_CreateWorkflow_Unity_Usages_of_Symbol,
                    dataContext.GetComponent<IActionDefs>().GetActionId<GoToUnityUsagesAction>(),
                    NavigationActionGroup.Important, execution);
            }
        }

        protected override void ExecuteSearchRequest(IDataContext context, SearchRequest searchRequest, INavigationExecutionHost host)
        {
            var provider = searchRequest.Solution.GetComponent<FindUsagesAsyncViewProviderBase>();
            var viewFactory = provider.GetFactoryShowUsages(searchRequest, host, this);
            var executer = new SearchRequestExecuter(context, searchRequest, this, host, viewFactory);
            executer.Execute();
        }
    }
}