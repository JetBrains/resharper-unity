using JetBrains.Application.changes;
using JetBrains.Application.Components;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
     [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class TestUnityExternalFilesModuleProcessor : UnityExternalFilesModuleProcessor
    {
        public TestUnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution,
                                                     ChangeManager changeManager,
                                                     IPsiModules psiModules,
                                                     PackageManager packageManager,
                                                     IShellLocks locks,
                                                     IFileSystemTracker fileSystemTracker,
                                                     IProjectFileExtensions projectFileExtensions,
                                                     UnityExternalPsiSourceFileFactory psiSourceFileFactory,
                                                     UnityExternalFilesModuleFactory moduleFactory,
                                                     UnityExternalFilesIndexDisablingStrategy indexDisablingStrategy,
                                                     ILazy<UnityAssetInfoCollector> usageStatistics,
                                                     AssetIndexingSupport assetIndexingSupport,
                                                     UnityExternalProjectFileTypes externalProjectFileTypes)
            : base(lifetime, logger, solution, changeManager, psiModules, packageManager, locks, fileSystemTracker,
                projectFileExtensions, psiSourceFileFactory, moduleFactory, indexDisablingStrategy, usageStatistics, assetIndexingSupport, externalProjectFileTypes)
        {
        }

        public override void OnHasUnityReference()
        {
        }
    }
}