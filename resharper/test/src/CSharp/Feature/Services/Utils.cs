using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.ProjectImplementation;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services
{
    public class Utils
    {
        //todo move this logic somewhere to base classes API
        //cleans up all resolved results and references. 
        public static void CleanupOldUnityReferences(ISolution solution)
        {
            var targetFrameworkScopes = solution.GetComponent<ResolveContextManager>().EnumerateAllScopes();

            using (WriteLockCookie.Create())
            {
                foreach (var targetFrameworkScope in targetFrameworkScopes)
                {
                    if (targetFrameworkScope is ProjectTargetFrameworkScope projectScope)
                    {
                        // These methods are marked as obsolete to make sure they're only used from tests. We have
                        // warnings as errors on, so disable the warning
#pragma warning disable 618
                        projectScope.RemoveAllProjectReferences();
                        projectScope.RemoveAllResolveResults();
#pragma warning restore 618
                    }
                }
            }
        }
    }
}