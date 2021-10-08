using System;
using JetBrains.Annotations;
using JetBrains.DocumentManagers;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    public class UnityExternalPsiSourceFile : PsiSourceFileFromPath, IExternalPsiSourceFile
    {
        public UnityExternalPsiSourceFile([NotNull] IProjectFileExtensions projectFileExtensions,
                                          [NotNull] PsiProjectFileTypeCoordinator projectFileTypeCoordinator,
                                          [NotNull] IPsiModule module,
                                          [NotNull] VirtualFileSystemPath path,
                                          [NotNull] Func<PsiSourceFileFromPath, bool> validityCheck,
                                          [NotNull] Func<PsiSourceFileFromPath, IPsiSourceFileProperties> propertiesFactory,
                                          [NotNull] DocumentManager documentManager,
                                          [NotNull] IModuleReferenceResolveContext resolveContext)
            : base(projectFileExtensions, projectFileTypeCoordinator, module, path, validityCheck, propertiesFactory,
                documentManager, resolveContext)
        {
        }
    }
}