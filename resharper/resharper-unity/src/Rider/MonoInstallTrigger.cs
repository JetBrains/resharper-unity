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
                            var parsedVersion = string.Empty; 
                            try
                            {
                                parsedVersion = ProcessOutputUtil.ExtractMonoVersion(runtime.ExePath);
                                
                            }
                            catch (Exception e)
                            {
                                logger.Warn(e);
                            }
                            
                            // if we fail to parse version - consider it is old
                            if (Version.TryParse(parsedVersion, out var version))
                                return version.Major >= 5 && version.Minor >= 16; // mono 5.16+ supports C# 7.3
                            
                            logger.Warn("Failed to parse ProcessOutputUtil.ExtractMonoVersion output.");
                            return false;

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
            // Either all projects are not loaded, when msbuild is not found or 
            // Unity, which requires new MSBuild with C# 7.3 support, writes TargetFrameworkVersion 4.7.1 to csproj
            using (solution.Locks.UsingReadLock())
            {
                var projects = solution.GetAllProjects();
                return 
                    projects.All(project => !project.GetAllTargetFrameworks().Any()) ||
                    projects.Any(project => 
                        project.TargetFrameworkIds
                            .Any(x => x.IsNetFramework && x.Version.Major >= 4 && x.Version.Minor >= 7 && x.Version.MajorRevision >= 1));
            }
        }
    }
}