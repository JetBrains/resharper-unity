#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ExternalSources.ILViewer.Extensibility;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ILViewer;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProjectOutputProvider(UnitySolutionTracker unitySolutionTracker) : IProjectOutputProvider
{
    public int Priority => 100;

    public bool IsApplicable(IProject project, TargetFrameworkId targetFrameworkId)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return unitySolutionTracker.IsUnityGeneratedProject.Maybe.Value;
#pragma warning restore CS0618 // Type or member is obsolete
    }
    
    public VirtualFileSystemPath GetOutputFilePath(IProject project, TargetFrameworkId targetFrameworkId)
    {
        var dllName = project.GetOutputFilePath(targetFrameworkId).Name;
        if (dllName.IsNullOrEmpty()) // Misc Project
            return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);
        return project.Location.Combine("Library").Combine("ScriptAssemblies").Combine(dllName);
    }
}