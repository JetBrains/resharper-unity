using JetBrains.Application.FileSystemTracker;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    // This module contains interesting Unity files that aren't part of an existing project. This includes .unity files
    // (so we can create references from YAML to methods) and .cs.meta files (so we can build an index of GUIDs for
    // MonoScript assets which then allows us to locate the methods referenced in YAML).
    // The files are added, removed and updated by UnityExternalFilesModuleProcessor
    [PsiModuleFactory]
    public class UnityExternalFilesModuleFactory : IPsiModuleFactory
    {
        public UnityExternalFilesModuleFactory(Lifetime lifetime, ISolution solution,
                                               IFileSystemTracker fileSystemTracker)
        {
            // TODO: Conditionally create this module
            // See changeBuilder.AddAssembly(module, ADDED)
            PsiModule = new UnityExternalFilesPsiModule(solution, "Unity external files", "UnityExternalFilesPsiModule",
                TargetFrameworkId.Default);

            Modules = new HybridCollection<IPsiModule>(PsiModule);
        }

        public HybridCollection<IPsiModule> Modules { get; }

        public UnityExternalFilesPsiModule PsiModule { get; }
    }
}
