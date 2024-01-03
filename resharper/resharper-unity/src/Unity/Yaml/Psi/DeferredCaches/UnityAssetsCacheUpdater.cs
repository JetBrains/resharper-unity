using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches
{
    [SolutionComponent]
    public class UnityAssetsCacheUpdater
    {
        public UnityAssetsCacheUpdater(Lifetime lifetime, ISolution solution,
                                       DeferredCacheController deferredCacheController,
                                       AssetIndexingSupport assetIndexingSupport, IShellLocks locks,
                                       ChangeManager changeManager, UnityExternalFilesModuleFactory factory)
        {
            var module = factory.PsiModule;
            assetIndexingSupport.IsEnabled.Change.Advise_NoAcknowledgement(lifetime, (handler) =>
            {
                if (handler.HasNew && handler.HasOld && handler.New == handler.Old)
                    return;

                locks.ExecuteOrQueueReadLockEx(lifetime, "Unity asset indexing cache state change", () =>
                {
                    var psiSourceFiles = module.SourceFiles.ToList();
                    if (psiSourceFiles.Any())
                    {
                        locks.ExecuteWithWriteLock(() =>
                        {
                            deferredCacheController.Invalidate<UnityAssetsCache>();

                            // TODO: Why is this being run through ChangeManager?
                            changeManager.ExecuteAfterChange(() =>
                            {
                                var changeBuilder = new PsiModuleChangeBuilder();
                                foreach (var sourceFile in psiSourceFiles)
                                {
                                    if (sourceFile.IsValid())
                                        changeBuilder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Modified);
                                }

                                changeManager.OnProviderChanged(solution, changeBuilder.Result,
                                    SimpleTaskExecutor.Instance);
                            });
                        });
                    }
                });
            });
        }
    }
}