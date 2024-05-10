using System;
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [MiscFilesProjectPsiModuleProvider(Instantiation.DemandAnyThread)]
    public class UnityMiscFilesProjectPsiModuleProvider : IMiscFilesProjectPsiModuleProvider
    {
        private readonly UnityExternalFilesModuleFactory myModuleFactory;

        public UnityMiscFilesProjectPsiModuleProvider(UnityExternalFilesModuleFactory moduleFactory)
        {
            myModuleFactory = moduleFactory;
        }

        public void Dispose() { }

        public IEnumerable<IPsiModule> GetModules() => new[] { myModuleFactory.PsiModule };

        public IEnumerable<IPsiSourceFile> GetPsiSourceFilesFor(IProjectFile projectFile)
        {
            if (projectFile == null)
                throw new ArgumentNullException(nameof(projectFile));
            Assertion.Assert(projectFile.IsValid());

            if (myModuleFactory.PsiModule.TryGetFileByPath(projectFile.Location, out var file))
                return new[] {file};

            return EmptyList<IPsiSourceFile>.Instance;
        }

        public void OnProjectFileChanged(IProjectFile? projectFile, PsiModuleChange.ChangeType changeType,
                                         PsiModuleChangeBuilder changeBuilder, VirtualFileSystemPath oldLocation)
        {
            if (projectFile == null)
                return;

            var module = myModuleFactory.PsiModule;
            switch (changeType)
            {
                case PsiModuleChange.ChangeType.Added:
                    // We don't normally do anything when a Misc File is added. All files are added by
                    // UnityExternalFilesModuleProcessor. The only time we want to do this is at initial solution load,
                    // where cached project file instances are pushed through the change manager before the module is
                    // populated. If we don't create these files on demand, the platform will create a default PSI
                    // source file that we don't have any control of.
                    // Note also that we can't inject UnityExternalFilesModuleProcessor, or it creates a circular ref.
                    // Note also that there is a bug in MiscFilesProjectProjectPsiModuleHandler.OnProjectFileChanged.
                    // It won't process all cached project files. It handles the first one, adds a change into the
                    // change builder (with a new PSI source file) and for subsequent files, it checks if there ar *any*
                    // file changes in the builder, not a file change for the specific current file. This doesn't affect
                    // what we're doing here, but it produces confusing results when trying to debug
                    var moduleProcessor = projectFile.GetSolution().GetComponent<UnityExternalFilesModuleProcessor>();
                    moduleProcessor.TryAddExternalPsiSourceFileForMiscFilesProjectFile(changeBuilder, projectFile);
                    break;

                case PsiModuleChange.ChangeType.Removed:
                    // Do nothing. We only remove source files if the underlying file itself has been removed, which is
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