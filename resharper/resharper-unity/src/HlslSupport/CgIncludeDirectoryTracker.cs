using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Threading.Tasks;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [SolutionComponent]
    public class CgIncludeDirectoryTracker
    {
        private const string CG_FOLDER_PATH = "CgIncludeFolderPath";

        public CgIncludeDirectoryTracker(Lifetime lifetime, UnityReferencesTracker unityReferencesTracker,
            SolutionCaches solutionCaches, IShellLocks shellLocks,
            CppGlobalCacheImpl cppGlobalCache, UnityVersion unityVersion, ILogger logger)
        {
            unityReferencesTracker.HasUnityReference.AdviseOnce(lifetime, _ =>
            {
                if (solutionCaches.PersistentProperties.TryGetValue(CG_FOLDER_PATH, out var result))
                {
                    var oldPath = FileSystemPath.TryParse(result, FileSystemPathInternStrategy.INTERN);
                    var newPath = GetCgIncludeFolderPath(unityVersion) ?? FileSystemPath.Empty;
                    if (!oldPath.Equals(newPath))
                    {
                        cppGlobalCache.IsInitialUpdateFinished.Change.Advise(lifetime, v =>
                        {
                            if (v.HasNew && v.New)
                            {
                                shellLocks.Tasks.StartNew(lifetime, Scheduling.MainGuard, TaskPriority.High, () =>
                                {
                                    logger.Verbose("Dropping C++ cache, because Unity version is changed");
                                    cppGlobalCache.ResetCache();
                                    solutionCaches.PersistentProperties[CG_FOLDER_PATH] = newPath.FullPath;
                                });
                            }
                        });
                    }
                }
                else
                {
                    solutionCaches.PersistentProperties[CG_FOLDER_PATH] =
                        (GetCgIncludeFolderPath(unityVersion) ?? FileSystemPath.Empty).FullPath;
                }
            });
        }


        public static FileSystemPath GetCgIncludeFolderPath(UnityVersion unityVersion)
        {
            var path = unityVersion.GetCurrentUnityPath();
            if (path != null)
            {
                return path.Parent.Combine("Data").Combine("CGIncludes");
            }

            return null;
        }
    }
}