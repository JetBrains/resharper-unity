using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.DocumentManagers;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
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
        private readonly DocumentManager myDocumentManager;
        private readonly IPsiSourceFileProperties myPsiSourceFileProperties;

        public UnityYamlPsiSourceFileFactory(IProjectFileExtensions projectFileExtensions,
                                             PsiProjectFileTypeCoordinator projectFileTypeCoordinator,
                                             UnityYamlSupport unityYamlSupport,
                                             DocumentManager documentManager)
        {
            myProjectFileExtensions = projectFileExtensions;
            myProjectFileTypeCoordinator = projectFileTypeCoordinator;
            myDocumentManager = documentManager;
            
            myPsiSourceFileProperties = new UnityExternalFileProperties(unityYamlSupport.IsYamlParsingEnabled);
        }

        public IPsiProjectFile CreatePsiProjectFile(IPsiModule psiModule, IProjectFile projectFile)
        {
            return new UnityYamlAssetPsiSourceFile(projectFile, myProjectFileExtensions, myProjectFileTypeCoordinator,
                psiModule, projectFile.Location, PropertiesFactory, myDocumentManager,
                UniversalModuleReferenceContext.Instance);
        }

        public IExternalPsiSourceFile CreateExternalPsiSourceFile(IPsiModule psiModule, FileSystemPath path)
        {
            return new UnityYamlExternalPsiSourceFile(myProjectFileExtensions, myProjectFileTypeCoordinator, psiModule,
                path, PropertiesFactory, myDocumentManager, UniversalModuleReferenceContext.Instance);
        }

        private IPsiSourceFileProperties PropertiesFactory(PsiSourceFileFromPath psiSourceFile) =>
            myPsiSourceFileProperties;
    }
}