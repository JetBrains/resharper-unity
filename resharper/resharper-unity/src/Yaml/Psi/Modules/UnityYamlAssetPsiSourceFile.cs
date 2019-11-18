using System;
using JetBrains.Annotations;
using JetBrains.DocumentManagers;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    // ReSharper doesn't want us to use project files. See UnityExternalFilesModuleProcessor
    public class UnityYamlAssetPsiSourceFile : PsiSourceFileFromPath, IExternalPsiSourceFile
#if RIDER
        , IPsiProjectFile
#endif
    {
        public UnityYamlAssetPsiSourceFile([NotNull] IProjectFile projectFile,
                                           [NotNull] IProjectFileExtensions projectFileExtensions,
                                           [NotNull] PsiProjectFileTypeCoordinator projectFileTypeCoordinator,
                                           [NotNull] IPsiModule module, FileSystemPath path,
                                           [NotNull] Func<PsiSourceFileFromPath, IPsiSourceFileProperties> propertiesFactory,
                                           [NotNull] DocumentManager documentManager,
                                           [NotNull] IModuleReferenceResolveContext resolveContext)
            : base(projectFileExtensions, projectFileTypeCoordinator, module, path, JetFunc<PsiSourceFileFromPath>.True,
                propertiesFactory, documentManager, resolveContext)
        {
#if RIDER
            ProjectFile = projectFile;
#endif
        }

#if RIDER
        public IProjectFile ProjectFile { get; }
#endif
    }
}