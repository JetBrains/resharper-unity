#nullable enable
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Runtime
{
    [SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
    public class UnityBundledSdkRefresher
    {
        public UnityBundledSdkRefresher(Lifetime lifetime,
                                         ISolution solution,
                                         UnityVersion unityVersion)
        {
            unityVersion.ActualAppPathForSolution.Advise(lifetime, appPath =>
            {
                var contentPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
                if (UnityBundledSdkLocator.HasBundledSdkWithMsBuild(contentPath))
                    solution.GetComponent<ISolutionToolset>().Refresh();
            });
        }
    }
}
