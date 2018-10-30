using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
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
            // NOTE: This module implementation has several limitations:
            // 1) It will only track file system changes for added files if they live under the solution folder. This is
            //    ok for Assets and Packages, but will probably cause problem with file based packages
            // 2) It doesn't let us add a module reference, which might cause problems when we add a reference from a
            //    .unity file (in this module) to a method in a C# project module

            // TODO: Conditionally create the module
            // I think we can do this by adding it, then doing changeBuilder.AddAssemblyChange(module, ADDED). See the
            // ExternalAnnotationsModuleFactory
            PsiModule = new PsiModuleOnFileSystemPaths(solution, "Unity external files", "UnityExternalFilesPsiModule",
                TargetFrameworkId.Default, fileSystemTracker, lifetime);
            Modules = new HybridCollection<IPsiModule>(PsiModule);
        }

        public PsiModuleOnFileSystemPaths PsiModule { get; }
        public HybridCollection<IPsiModule> Modules { get; }
    }
}
