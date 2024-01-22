using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Features.Navigation.Features.FindUsages;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Navigation.FindUsages
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
                        || declaredElement is IField field && api.IsSerialisedField(field).HasFlag(SerializedFieldStatus.SerializedField)
                        || declaredElement is ITypeElement type && api.IsUnityType(type))
                    {
                        return string.Format(Strings.UnityFindUsagesProvider_GetNotFoundMessage_SearchRequestLocalizedTitle_are_only_implicit_, request.Title);
                    }
                }
            }

            return base.GetNotFoundMessage(request);
        }
    }
}