using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
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
    [SolutionComponent]
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
                                                     UnityAssetInfoCollector usageStatistics,
                                                     AssetIndexingSupport assetIndexingSupport,
                                                     UnityIndexedExternalProjectFileTypeFilter indexedExternalProjectFileTypeFilter)
            : base(lifetime, logger, solution, changeManager, psiModules, packageManager, locks, fileSystemTracker,
                projectFileExtensions, psiSourceFileFactory, moduleFactory, indexDisablingStrategy, usageStatistics, assetIndexingSupport, indexedExternalProjectFileTypeFilter)
        {
        }

        public override void OnHasUnityReference()
        {
        }
    }
}