using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    // This module contains interesting Unity files that aren't part of an existing project. This includes .unity files
    // (so we can create references from YAML to methods) and .cs.meta files (so we can build an index of GUIDs for
    // MonoScript assets which then allows us to locate the methods referenced in YAML).
    // The files are added, removed and updated by UnityExternalFilesModuleProcessor
    [PsiModuleFactory]
    public class UnityExternalFilesModuleFactory : IPsiModuleFactory
    {
        public UnityExternalFilesModuleFactory(Lifetime lifetime, ISolution solution,
                                               IFileSystemTracker fileSystemTracker, ChangeManager changeManager)
        {
            // NOTE: It would be nice to conditionally create this, but UnityExternalFilesModuleProcessor, and more
            // importantly its base class AdditionalFilesModuleFactoryBase require an IPsiModuleOnFileSystemPaths as a
            // constructor parameter. Otherwise, changeBuilder.AddAssemblyChange(module, ADDED) would do the trick.
            PsiModule = new UnityExternalFilesPsiModule(solution, "Unity external files", "UnityExternalFilesPsiModule",
                TargetFrameworkId.Default, fileSystemTracker, lifetime);
            Modules = new HybridCollection<IPsiModule>(PsiModule);
        }

        public UnityExternalFilesPsiModule PsiModule { get; }
        public HybridCollection<IPsiModule> Modules { get; }
    }
}
