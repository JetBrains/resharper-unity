using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Features.Navigation.Features.FindUsages;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class UnityFindUsagesProvider : FindUsagesProvider
    {
        private readonly SolutionsManager mySolutionsManager;

        public UnityFindUsagesProvider(IFeaturePartsContainer manager, SolutionsManager solutionsManager)
            : base(manager)
        {
            mySolutionsManager = solutionsManager;
        }

        public override string GetNotFoundMessage(SearchRequest request)
        {
            var i = 0;
            IDeclaredElement declaredElement = null;
            foreach (var searchTarget in request.SearchTargets)
            {
                if (i > 1)
                    return base.GetNotFoundMessage(request);
                declaredElement = (searchTarget as DeclaredElementEnvoy<IDeclaredElement>)?.GetValidDeclaredElement();
                i++;
            }
            if (declaredElement != null)
            {
                var api = mySolutionsManager.Solution?.GetComponent<UnityApi>();
                if (api != null)
                {
                    if (declaredElement is IMethod method && api.IsEventFunction(method)
                        || declaredElement is IField field && api.IsSerialisedField(field)
                        || declaredElement is ITypeElement type && api.IsUnityType(type))
                    {
                        return request.Title + " are only implicit.";
                    }
                }

                
            }

            return base.GetNotFoundMessage(request);
        }
    }
}