using System;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class MonoInstallTrigger
    {
        public MonoInstallTrigger(Lifetime lifetime, ILogger logger, UnityHost host,
            ISolution solution,
            UnitySolutionTracker unitySolutionTracker)
        {
            unitySolutionTracker.IsUnityGeneratedProject.WhenTrue(lifetime,
                (lt =>
                {
                    if (PlatformUtil.RuntimePlatform != PlatformUtil.Platform.MacOsX)
                        return;

                    bool shouldShow;

                    using (solution.Locks.UsingReadLock())
                    {
                        shouldShow = solution.GetAllProjects()
                            .Any(a => a.TargetFrameworkIds.Any(x => x.Version.Major >= 4 && x.Version.Minor >= 7)
//                                        && a.TargetFrameworkIds.Any(t =>
//                                            (a.ProjectProperties.ActiveConfigurations.TryGetConfiguration(t) as
//                                                ICSharpProjectConfiguration)
//                                            .IfNotNull(s => s.LanguageVersion < CSharpLanguageVersion.CSharp73))
                            );

                    }

                    if (!shouldShow) return;

                    var wellKnownMonoRuntimes = MonoRuntimeDetector.DetectWellKnownMonoRuntimes();
                    if (!wellKnownMonoRuntimes.Any())
                    {
                        host.PerformModelAction(model => model.ShowInstallMonoDialog());
                        return;
                    }

                    var installed =
                        wellKnownMonoRuntimes
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
                                return version.Major>=5 && version.Minor>=16;
                            });
                            
                    if (!installed)
                        host.PerformModelAction(model => model.ShowInstallMonoDialog());
                }));
        }
    }
}