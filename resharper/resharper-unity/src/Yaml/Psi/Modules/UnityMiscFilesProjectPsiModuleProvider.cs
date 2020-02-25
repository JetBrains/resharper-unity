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

        }
    }
}