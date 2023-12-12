using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Threading.Tasks;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport
{
    [SolutionComponent]
    public class CgIncludeDirectoryTracker
    {
        private const string CG_INCLUDE_DIRECTORY_PATH = "CgIncludeFolderPath";

        public CgIncludeDirectoryTracker(Lifetime lifetime, UnitySolutionTracker unitySolutionTracker,
            SolutionCaches solutionCaches, IShellLocks shellLocks, ISolutionLoadTasksScheduler scheduler,
            CppGlobalCacheImpl cppGlobalCache, ILogger logger, ICgIncludeDirectoryProvider cgIncludeDirectoryProvider)
        {
            scheduler.EnqueueTask(new SolutionLoadTask("InitCgIncludeDirectoryTracker", SolutionLoadTaskKinds.PreparePsiModules,
                () =>
                {
                    unitySolutionTracker.HasUnityReference.WhenTrue(lifetime, _ =>
                    {
                        if (solutionCaches.PersistentProperties.TryGetValue(CG_INCLUDE_DIRECTORY_PATH, out var result))
                        {
                            var oldPath = VirtualFileSystemPath.TryParse(result, InteractionContext.SolutionContext);
                            var newPath = cgIncludeDirectoryProvider.GetCgIncludeFolderPath();
                            if (!oldPath.Equals(newPath))
                            {
                                cppGlobalCache.IsCacheStarted.Change.Advise(lifetime, v =>
                                {
                                    if (v.HasNew && v.New)
                                    {
                                        shellLocks.Tasks.StartNew(lifetime, Scheduling.MainGuard, TaskPriority.High, () =>
                                        {
                                            logger.Verbose("Dropping C++ cache, because Unity version is changed");
                                            cppGlobalCache.ResetCache();
                                            solutionCaches.PersistentProperties[CG_INCLUDE_DIRECTORY_PATH] = newPath.FullPath;
                                        });
                                    }
                                });
                            }
                        }
                        else
                        {
                            solutionCaches.PersistentProperties[CG_INCLUDE_DIRECTORY_PATH] = cgIncludeDirectoryProvider.GetCgIncludeFolderPath().FullPath;
                        }
                    });
                }));
        }
    }
}