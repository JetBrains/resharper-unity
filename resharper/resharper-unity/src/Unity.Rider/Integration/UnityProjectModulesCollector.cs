using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Feature.Services.Project;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProjectModulesCollector : SolutionUsagesCollector
{
    private static readonly string[] ourAllowedBuildTargets =
    [
        "StandaloneOSX", "StandaloneOSXUniversal", "StandaloneOSXIntel", "StandaloneWindows", "WebPlayer",
        "WebPlayerStreamed", "iOS", "PS3", "XBOX360", "Android", "StandaloneLinux", "StandaloneWindows64", "WebGL",
        "WSAPlayer", "StandaloneLinux64", "StandaloneLinuxUniversal", "WP8Player", "StandaloneOSXIntel64",
        "BlackBerry", "Tizen", "PSP2", "PS4", "PSM", "XboxOne", "SamsungTV", "N3DS", "WiiU", "tvOS", "Switch",
        "Lumin", "Stadia", "CloudRendering", "LinuxHeadlessSimulation", "GameCoreXboxSeries", "GameCoreScarlett",
        "GameCoreXboxOne", "PS5", "EmbeddedLinux", "QNX", "VisionOS", "ReservedCFE", "NoTarget", "iPhone", "BB10",
        "MetroPlayer"
    ];

    private static readonly string[] ourAllowedBuildTargetGroups =
    [
        "Unknown", "Standalone", "WebPlayer", "iPhone", "iOS", "PS3", "XBOX360", "Android", "WebGL", "WSA", "Metro",
        "WP8", "BlackBerry", "Tizen", "PSP2", "PS4", "PSM", "XboxOne", "SamsungTV", "N3DS", "WiiU", "tvOS",
        "Facebook", "Switch", "Lumin", "Stadia", "CloudRendering", "LinuxHeadlessSimulation", "GameCoreScarlett",
        "GameCoreXboxSeries", "GameCoreXboxOne", "PS5", "EmbeddedLinux", "QNX", "VisionOS", "ReservedCFE"
    ];

    private static readonly string[] ourAllowedInstalledModules =
        ["LinuxStandalone", "AppleTV", "iOS", "Android", "WindowsStandalone", "VisionOS", "OSXStandalone", "WebGL"];

    private readonly EventLogGroup myGroup;
    private readonly IFeaturesStartupMonitor myMonitor;
    private readonly ISolution mySolution;
    private readonly UnitySolutionTracker myUnitySolutionTracker;
    private readonly EventId1<string> myActiveBuildTargetEvent;
    private readonly EventId1<IList<string>> myInstalledModulesEvent;
    private readonly EventId1<string> myInstalledModuleEvent;

    private readonly EventId1<string> myBuildTargetGroupWithIcons;
    private readonly EventId1<IList<string>> myBuildTargetGroupsWithIcons;

    private readonly UnityProjectModulesTracker myProjectModulesTracker; 
    
    public override EventLogGroup GetGroup()
    {
        return myGroup;
    }

    public UnityProjectModulesCollector(FeatureUsageLogger featureUsageLogger, IFeaturesStartupMonitor monitor,
        UnitySolutionTracker unitySolutionTracker, ISolution solution,
        UnityProjectModulesTracker projectModulesTracker)
    {
        myMonitor = monitor;
        myUnitySolutionTracker = unitySolutionTracker;
        mySolution = solution;
        myProjectModulesTracker = projectModulesTracker;
        myGroup = new EventLogGroup("dotnet.unity.platforms", "Unity Available Platforms Information", 2,
            featureUsageLogger);
        myActiveBuildTargetEvent = myGroup.RegisterEvent("activeBuildTarget",
            "Active Build Target: currently selected build target in Unity Editor"
            , EventFields.String("id", "Name", ourAllowedBuildTargets));

        myInstalledModuleEvent = myGroup.RegisterEvent("installedBuildModule",
            "Installed build target module, which can be used as a build target.",
            EventFields.String("name", "Installed Editor Module", ourAllowedInstalledModules));
        myInstalledModulesEvent = myGroup.RegisterEvent("installedBuildModules",
            "Installed build target modules, which can be used as build targets.",
            EventFields.StringList("name", "Installed Editor Module", ourAllowedInstalledModules));

        myBuildTargetGroupWithIcons = myGroup.RegisterEvent("buildTargetGroupWithIcons",
            "This target includes a custom icon, indicating potential use as build target.",
            EventFields.String("name", "Build Target Group", ourAllowedBuildTargetGroups));
        myBuildTargetGroupsWithIcons = myGroup.RegisterEvent("buildTargetGroupsWithIcons",
            "These targets include a custom icon, indicating potential use as build targets.",
            EventFields.StringList("buildTargetNames", "Build Target Group", ourAllowedBuildTargetGroups));
    }


    public override async Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
    {
        return await lifetime.StartMainReadAsync(async () =>
        {
            await myMonitor.FullStartupFinished.NextTrueValueAsync(lifetime);

            var isUnity = myUnitySolutionTracker.HasUnityReference.Value ||
                          myUnitySolutionTracker.IsUnityProjectFolder.Value;

            if (!isUnity) return EmptySet<MetricEvent>.InstanceSet;

            if(!myProjectModulesTracker.UnityPlatformInfo.HasValue())
                await myProjectModulesTracker.UnityPlatformInfo.NextNotNullValueAsync(lifetime);

            return await mySolution.GetPsiServices().Files.CommitWithRetryBackgroundRead(lifetime, () =>
            {
                var hashSet = new HashSet<MetricEvent>();
                if (!myProjectModulesTracker.UnityPlatformInfo.HasValue())
                    return hashSet;

                var platformInfo = myProjectModulesTracker.UnityPlatformInfo.Value;
                if (!string.IsNullOrEmpty(platformInfo.ActiveBuildTarget))
                    hashSet.Add(myActiveBuildTargetEvent.Metric(platformInfo.ActiveBuildTarget));

                var platformInfoInstalledModules = platformInfo.InstalledModules;
                if (platformInfoInstalledModules.Count > 0)
                {
                    platformInfoInstalledModules.Sort();
                    
                    hashSet.Add(myInstalledModulesEvent.Metric(platformInfoInstalledModules));
                    foreach (var moduleName in platformInfoInstalledModules)
                        hashSet.Add(myInstalledModuleEvent.Metric(moduleName));
                }

                var platformInfoBuildTargetGroupsWithIcons = platformInfo.BuildTargetGroupsWithIcons;
                if (platformInfoBuildTargetGroupsWithIcons.Count > 0)
                {
                    platformInfoBuildTargetGroupsWithIcons.Sort();
                    hashSet.Add(myBuildTargetGroupsWithIcons.Metric(platformInfoBuildTargetGroupsWithIcons));
                    foreach (var moduleName in platformInfoBuildTargetGroupsWithIcons)
                        hashSet.Add(myBuildTargetGroupWithIcons.Metric(moduleName));
                }
                
                return hashSet;
            });
        });
    }
}