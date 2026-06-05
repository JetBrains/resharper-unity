#nullable enable
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.BuildTools;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Runtime;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityBuildToolSelector(SolutionsManager solutionsManager) : IBuildToolSelector
{
    public IBuildTool? Select(VirtualFileSystemPath solutionFilePath,
        IReadOnlyCollection<VirtualFileSystemPath> projectFiles,
        IReadOnlyCollection<IBuildTool> buildTools)
    {
        var solution = solutionsManager.Solution;
        if (solution == null)
            return null;

        var unityVersion = solution.TryGetComponent<UnityVersion>();
        if (unityVersion == null)
            return null;
            
        var appPath = unityVersion.ActualAppPathForSolution.Maybe.ValueOrDefault;
        var contentPath = UnityInstallationFinder.GetApplicationContentsPath(appPath);

        if (contentPath.IsEmpty)
            return null;

        // when we have Unity build tool, we should use it
        return buildTools.FirstOrDefault(buildTool => buildTool.Directory.StartsWith(contentPath));
    }
}