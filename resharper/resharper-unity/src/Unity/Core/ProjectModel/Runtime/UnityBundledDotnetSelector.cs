#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.platforms;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Runtime
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    internal sealed class UnityBundledDotnetSelector(SolutionsManager solutionsManager) : IDotNetCoreSelector
    {
        public IDotNetCoreCli? Select(IReadOnlyCollection<IDotNetCoreCli> toolsets, VirtualFileSystemPath? solutionRoot)
        {
            var solution = solutionsManager.Solution;
            if (solution == null)
                return null;

            var unityVersion = solution.TryGetComponent<UnityVersion>();
            if (unityVersion == null)
                return null;
            
            var appPath = unityVersion.ActualAppPathForSolution.Maybe.ValueOrDefault;
            var contentPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);
            if (contentPath.IsEmpty) return null;
            
            return toolsets.FirstOrDefault(toolset => toolset.SdkRootFolder.StartsWith(contentPath));
        }

        public int Priority => 0;
    }
}
