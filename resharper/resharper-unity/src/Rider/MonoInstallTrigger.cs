using System;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class MonoInstallTrigger
    {
        public MonoInstallTrigger(Lifetime lifetime, ILogger logger, ISolutionLoadTasksScheduler scheduler,
            ISolution solution, UnitySolutionTracker unitySolutionTracker, UnityHost host)
        {
            if (PlatformUtil.RuntimePlatform != PlatformUtil.Platform.MacOsX)
                return;

            scheduler.EnqueueTask(new SolutionLoadTask("Check mono runtime", SolutionLoadTaskKinds.AfterDone, () =>
            {
                if (!unitySolutionTracker.IsUnityGeneratedProject.Value)
                    return;

                if (!HasModernUnityProjects(solution))
                    return;

                solution.Locks.Tasks.Queue(lifetime, () =>
                {
                    var wellKnownMonoRuntimes = MonoRuntimeDetector.DetectWellKnownMonoRuntimes();
                    var installedValidMono = wellKnownMonoRuntimes
                        .Any(runtime =>
                        {
                            var version = new Version(0, 0); // if we fail to parse version - consider it is old
                            try
                            {
                                version = new Version(ProcessOutputUtil.ExtractMonoVersion(runtime.ExePath));
                            }
                            catch (Exception e)
                            {
                                logger.Warn(e);
                            }

                            return version.Major >= 5 && version.Minor >= 16;
                        });

                    if (!installedValidMono)
                    {
                        solution.Locks.ExecuteOrQueue(lifetime, "Show install mono dialog",
                            () => { host.PerformModelAction(model => model.ShowInstallMonoDialog()); });
                    }
                });
            }));
        }
    
        private static bool HasModernUnityProjects(ISolution solution)
        {
            using (solution.Locks.UsingReadLock())
            {
                foreach (var project in solution.GetAllProjects())
                {
                    var isNetFramework47 = project.TargetFrameworkIds
                        .Any(x => x.IsNetFramework && x.Version.Major >= 4 && x.Version.Minor >= 7);
                
                    if (isNetFramework47)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}