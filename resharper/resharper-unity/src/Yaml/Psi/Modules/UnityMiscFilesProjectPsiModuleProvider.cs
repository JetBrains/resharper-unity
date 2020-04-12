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

        public UnityMiscFilesProjectPsiModuleProvider(UnityExternalFilesModuleFactory moduleFactory)
        {
            myModuleFactory = moduleFactory;
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
            if (projectFile == null)
                return;
            
            var module = myModuleFactory.PsiModule;
            if (module == null)
                return;
            
            switch (changeType)
            {
                case PsiModuleChange.ChangeType.Added:
                case PsiModuleChange.ChangeType.Removed:
                    // Do nothing. We only add/remove source files if the underlying file itself has been removed, which is
                    // handled by UnityExternalFilesModuleProcessor and a file system watcher
                    break;
            
                case PsiModuleChange.ChangeType.Modified:
                    if (module.TryGetFileByPath(projectFile.Location, out var sourceFile))
                        changeBuilder.AddFileChange(sourceFile, changeType);
                    break;
            }
        }
    }
}