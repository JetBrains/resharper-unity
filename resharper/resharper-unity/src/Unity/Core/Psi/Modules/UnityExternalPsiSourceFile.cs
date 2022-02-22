using System;
using JetBrains.DocumentManagers;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    public class UnityExternalPsiSourceFile : PsiSourceFileFromPath, IExternalPsiSourceFile
    {
        public UnityExternalPsiSourceFile(VirtualFileSystemPath path,
                                          IPsiModule module,
                                          ProjectFileType projectFileType,
                                          Func<PsiSourceFileFromPath, bool> validityCheck,
                                          Func<PsiSourceFileFromPath, IPsiSourceFileProperties> propertiesFactory,
                                          IProjectFileExtensions projectFileExtensions,
                                          PsiProjectFileTypeCoordinator projectFileTypeCoordinator,
                                          DocumentManager documentManager,
                                          IModuleReferenceResolveContext resolveContext,
                                          CachedFileSystemData fileSystemData)
            : base(projectFileExtensions, projectFileTypeCoordinator, module, path, validityCheck, propertiesFactory,
                documentManager, resolveContext, fileSystemData)
        {
            LanguageType = projectFileType;
        }

        // We explicitly specify the file's ProjectFileType for several reasons:
        // * PsiSourceFileFromPath.LanguageType allocates, which is very expensive when we have tens of thousands of external files
        // * We need to treat ProjectSettings files as Yaml files instead of UnityYaml, which is the default for .asset
        //
        // Note that this property is ultimately responsible for the PsiLanguageType of this source file -
        // PsiSourceFileWithPath's implementation of IPsiSourceFile.PrimaryPsiLanguage defers to the ProjectFileType's
        // language service. Default handling of IProjectFileLanguageService.GetPsiLanguageType will ultimately return
        // that language's default PSI language. For the files we're interested in (Yaml/Meta, UnityYaml, AsmDef/AsmRef)
        // there is no special treatment for PSI or project files, so if we specify the right ProjectFileType here,
        // we'll get the equivalent PsiLanguageType for the file
        public override ProjectFileType LanguageType { get; }
    }
}