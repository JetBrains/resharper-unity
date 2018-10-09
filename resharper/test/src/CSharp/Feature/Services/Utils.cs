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
                        projectScope.RemoveAllProjectReferences();
                        projectScope.RemoveAllResolveResults();
                    }
                }
            }
        }
    }
}