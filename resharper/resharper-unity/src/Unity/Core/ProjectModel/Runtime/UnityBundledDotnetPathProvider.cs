#nullable enable
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.platforms;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Runtime
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityBundledDotnetPathProvider(SolutionsManager solutionsManager) : IDotNetCorePathProvider
    {
        public IReadOnlyList<VirtualFileSystemPath> GetPossibleDotNetPaths(IInteractionContext context)
        {
            var solution = solutionsManager.Solution;
            if (solution == null)
                return EmptyList<VirtualFileSystemPath>.Instance;
            
            var unityVersion = solution.TryGetComponent<UnityVersion>();
            if (unityVersion == null)
                return EmptyList<VirtualFileSystemPath>.Instance;
            
            var appPath = unityVersion.ActualAppPathForSolution.Maybe.ValueOrDefault;
            var contentPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);

            var dotnetFolder = UnityBundledSdkLocator.GetBundledDotnetFolder(contentPath);
            if (dotnetFolder.IsEmpty)
                return EmptyList<VirtualFileSystemPath>.Instance;

            return [dotnetFolder];
        }
    }
}
