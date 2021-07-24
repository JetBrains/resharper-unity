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
    public class UnityYamlExternalPsiSourceFile : PsiSourceFileFromPath, IExternalPsiSourceFile
    {
        public UnityYamlExternalPsiSourceFile([NotNull] IProjectFileExtensions projectFileExtensions,
                                              [NotNull] PsiProjectFileTypeCoordinator projectFileTypeCoordinator,
                                              [NotNull] IPsiModule module, VirtualFileSystemPath path,
                                              [NotNull] Func<PsiSourceFileFromPath, IPsiSourceFileProperties> propertiesFactory,
                                              [NotNull] DocumentManager documentManager,
                                              [NotNull] IModuleReferenceResolveContext resolveContext)
            : base(projectFileExtensions, projectFileTypeCoordinator, module, path, JetFunc<PsiSourceFileFromPath>.True,
                propertiesFactory, documentManager, resolveContext)
        {
        }
    }
}