using System;
using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [MiscFilesProjectPsiModuleProvider]
    public class UnityMiscFilesProjectPsiModuleProvider : IMiscFilesProjectPsiModuleProvider
    {
        private readonly UnityExternalFilesModuleFactory myModuleFactory;
        private readonly UnityYamlPsiSourceFileFactory myPsiSourceFileFactory;
        private readonly AssetSerializationMode myAssetSerializationMode;

        public UnityMiscFilesProjectPsiModuleProvider(UnityExternalFilesModuleFactory moduleFactory,
                                                      UnityYamlPsiSourceFileFactory psiSourceFileFactory,
                                                      AssetSerializationMode assetSerializationMode)
        {
            myModuleFactory = moduleFactory;
            myPsiSourceFileFactory = psiSourceFileFactory;
            myAssetSerializationMode = assetSerializationMode;
        }

        public void Dispose() { }

        public IEnumerable<IPsiModule> GetModules()
        {
            var module = myModuleFactory.PsiModule;
            return module != null ? new[] {module} : EmptyArray<IPsiModule>.Instance;
        }

        public IEnumerable<IPsiSourceFile> GetPsiSourceFilesFor(IProjectFile projectFile)
        {
            if (projectFile == null)
                throw new ArgumentNullException(nameof(projectFile));
            Assertion.Assert(projectFile.IsValid(), "projectFile.IsValid()");

            var module = myModuleFactory.PsiModule;
            if (module != null && module.TryGetFileByPath(projectFile.Location, out var file))
                return new[] {file};

            return EmptyList<IPsiSourceFile>.Instance;
        }

        public void OnProjectFileChanged(IProjectFile projectFile, PsiModuleChange.ChangeType changeType,
                                         PsiModuleChangeBuilder changeBuilder, FileSystemPath oldLocation)
        {
            return;
            // if (projectFile == null)
            //     return;
            //
            // var module = myModuleFactory.PsiModule;
            // if (module == null)
            //     return;
            //
            // switch (changeType)
            // {
            //     case PsiModuleChange.ChangeType.Added:
            //         if (projectFile.Location.IsInterestingAsset() && myAssetSerializationMode.IsForceText &&
            //             !module.ContainsPath(projectFile.Location))
            //         {
            //             // Create the PsiSourceFile, add it to the module, add the change to the builder
            //             var psiSourceFile = myPsiSourceFileFactory.CreatePsiProjectFile(module, projectFile);
            //             module.Add(projectFile.Location, psiSourceFile, null);
            //             changeBuilder.AddFileChange(psiSourceFile, PsiModuleChange.ChangeType.Added);
            //         }
            //         break;
            //
            //     case PsiModuleChange.ChangeType.Removed:
            //         // Do nothing. We only remove source files if the underlying file itself has been removed, which is
            //         // handled by UnityExternalFilesModuleProcessor and a file system watcher
            //         break;
            //
            //     case PsiModuleChange.ChangeType.Modified:
            //         if (module.TryGetFileByPath(projectFile.Location, out var sourceFile))
            //             changeBuilder.AddFileChange(sourceFile, changeType);
            //         break;
            // }
        }
    }
}