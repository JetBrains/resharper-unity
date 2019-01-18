using System;
using JetBrains.DocumentManagers;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class UnityYamlPsiSourceFileFactory
    {
        private readonly IProjectFileExtensions myProjectFileExtensions;
        private readonly PsiProjectFileTypeCoordinator myProjectFileTypeCoordinator;
        private readonly UnityYamlSupport myUnityYamlSupport;
        private readonly DocumentManager myDocumentManager;

        public UnityYamlPsiSourceFileFactory(IProjectFileExtensions projectFileExtensions,
                                             PsiProjectFileTypeCoordinator projectFileTypeCoordinator,
                                             UnityYamlSupport unityYamlSupport,
                                             DocumentManager documentManager)
        {
            myProjectFileExtensions = projectFileExtensions;
            myProjectFileTypeCoordinator = projectFileTypeCoordinator;
            myUnityYamlSupport = unityYamlSupport;
            myDocumentManager = documentManager;
        }

        public IPsiProjectFile CreatePsiProjectFile(IPsiModule psiModule, IProjectFile projectFile)
        {
            var file = new UnityYamlAssetPsiSourceFile(projectFile, myProjectFileExtensions, myProjectFileTypeCoordinator,
                psiModule, projectFile.Location, Memoize(PropertiesFactory), myDocumentManager, UniversalModuleReferenceContext.Instance);
            // Prime the file system cache
            file.GetCachedFileSystemData();
            return file;
        }

        public IExternalPsiSourceFile CreateExternalPsiSourceFile(IPsiModule psiModule, FileSystemPath path)
        {
            var file = new UnityYamlExternalPsiSourceFile(myProjectFileExtensions, myProjectFileTypeCoordinator, psiModule,
                path, Memoize(PropertiesFactory), myDocumentManager, UniversalModuleReferenceContext.Instance);
            // Prime the file system cache
            file.GetCachedFileSystemData();
            return file;
        }

        // The PropertiesFactory passed to PsiSourceFileFromPath is called on EVERY access to IPsiSourceFile.Properties.
        // This function allows us to create a single instance for each file. The cache variable (as well as the func
        // parameter) are captured into a closure class. When the closure's is invoked, we can populate and return the
        // cached value
        private static Func<IPsiSourceFile, IPsiSourceFileProperties> Memoize(Func<IPsiSourceFile, IPsiSourceFileProperties> func)
        {
            IPsiSourceFileProperties cache = null;
            return sf => cache ?? (cache = func(sf));
        }

        private IPsiSourceFileProperties PropertiesFactory(IPsiSourceFile psiSourceFile)
        {
            var binaryUnityFileCache = psiSourceFile.GetSolution().GetComponent<BinaryUnityFileCache>();
            return new UnityExternalFileProperties(psiSourceFile, myUnityYamlSupport, binaryUnityFileCache);
        }
    }
}