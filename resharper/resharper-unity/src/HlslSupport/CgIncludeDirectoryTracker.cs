using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Threading.Tasks;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class CgIncludeDirectoryTracker
    {
        private const string CG_INCLUDE_DIRECTORY_PATH = "CgIncludeFolderPath";

        public CgIncludeDirectoryTracker(Lifetime lifetime, UnityReferencesTracker unityReferencesTracker,
            SolutionCaches solutionCaches, IShellLocks shellLocks, ISolutionLoadTasksScheduler scheduler,
            CppGlobalCacheImpl cppGlobalCache, UnityVersion unityVersion, ILogger logger)
        {
            scheduler.EnqueueTask(new SolutionLoadTask("InitCgIncludeDirectoryTracker", SolutionLoadTaskKinds.PreparePsiModules,
                () =>
                {
                    unityReferencesTracker.HasUnityReference.AdviseOnce(lifetime, _ =>
                    {
                        if (solutionCaches.PersistentProperties.TryGetValue(CG_INCLUDE_DIRECTORY_PATH, out var result))
                        {
                            var oldPath = FileSystemPath.TryParse(result, FileSystemPathInternStrategy.INTERN);
                            var newPath = GetCgIncludeFolderPath(unityVersion);
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
                            solutionCaches.PersistentProperties[CG_INCLUDE_DIRECTORY_PATH] = GetCgIncludeFolderPath(unityVersion).FullPath;
                        }
                    });
                }));

        }


        public static FileSystemPath GetCgIncludeFolderPath(UnityVersion unityVersion)
        {
            var path = unityVersion.GetActualAppPathForSolution();
            if (path.IsEmpty)
                return FileSystemPath.Empty;

            var contentPath = UnityInstallationFinder.GetApplicationContentsPath(path);
            return contentPath.Combine("CGIncludes");
        }
    }
}