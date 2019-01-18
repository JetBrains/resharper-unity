using JetBrains.Annotations;
using JetBrains.Application.FileSystemTracker;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    // This module contains interesting Unity files that aren't part of an existing project. This includes .unity files
    // (so we can create references from YAML to methods) and .cs.meta files (so we can build an index of GUIDs for
    // MonoScript assets which then allows us to locate the methods referenced in YAML).
    // The files are added, removed and updated by UnityExternalFilesModuleProcessor
    [SolutionComponent]
    public class UnityExternalFilesModuleFactory
    {
        public UnityExternalFilesModuleFactory(Lifetime lifetime, ISolution solution,
                                               IFileSystemTracker fileSystemTracker)
        {
            // TODO: Conditionally create this module
            // See changeBuilder.AddAssembly(module, ADDED)
            PsiModule = new UnityExternalFilesPsiModule(solution, "Unity external files", "UnityExternalFilesPsiModule",
                TargetFrameworkId.Default, lifetime);
        }

        [CanBeNull] public UnityExternalFilesPsiModule PsiModule { get; }
    }
}
