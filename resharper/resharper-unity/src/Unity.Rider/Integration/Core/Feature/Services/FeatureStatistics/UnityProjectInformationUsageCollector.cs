using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Feature.Services.Project;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Services.FeatureStatistics
{
    [SolutionComponent]
    public class UnityProjectInformationUsageCollector : SolutionUsagesCollector
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly FeaturesStartupMonitor myMonitor;

        private enum UnityProjectKind
        {
            Generated,
            Library,
            Sidecar,
            Other
        }
        
        private EventLogGroup myGroup;
        private readonly EventId1<UnityProjectKind> myProjectKindEvent;
        private readonly EventId2<string, bool> myUnityVersionEvent;
        public const string VersionRegex = @"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(?<type>[a-z])(?<revision>\d+)";

        public UnityProjectInformationUsageCollector(ISolution solution, UnitySolutionTracker unitySolutionTracker, FeaturesStartupMonitor monitor, FeatureUsageLogger featureUsageLogger)
        {
            mySolution = solution;
            myUnitySolutionTracker = unitySolutionTracker;
            myMonitor = monitor;
            myGroup = new EventLogGroup("dotnet.unity.projects", "Unity Project Information", 1, featureUsageLogger);
            myProjectKindEvent = myGroup.RegisterEvent("projectKind", "Project Kind", 
                EventFields.Enum<UnityProjectKind>("type", "Type"));
            
            myUnityVersionEvent = myGroup.RegisterEvent("unityVersion", "Project Unity Version", 
                EventFields.StringValidatedByRegexp("unityVersion", "Unity Version", VersionRegex),
                EventFields.Boolean("isCustomUnityVersion", "Custom Unity Build")); 

        }
        
        public override EventLogGroup GetGroup()
        {
            return myGroup;
        }

        public static (string, bool) GetUnityVersion(string versionInfo)
        {
            const string unknownVersion = "0.0.0f0";
            versionInfo = versionInfo ?? unknownVersion;
            var match = Regex.Match(versionInfo, VersionRegex);
            if (match.Success)
            {
                var matchedSubstring = match.Value;
                return (matchedSubstring, !matchedSubstring.Equals(versionInfo));
            }
            else
            {
                return (unknownVersion, false);
            }
        }

        public override Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
        {
            var tcs = lifetime.CreateTaskCompletionSource<ISet<MetricEvent>>(TaskCreationOptions.RunContinuationsAsynchronously);

            myMonitor.FullStartupFinished.AdviseUntil(lifetime, v =>
            {
                if (v)
                {
                    var (verifiedVersion, isCustom) = GetUnityVersion(UnityVersion.GetProjectSettingsUnityVersion(mySolution));
                    
                    var hashSet = new HashSet<MetricEvent>();
                    hashSet.Add(myProjectKindEvent.Metric(GetProjectType()));
                    hashSet.Add(myUnityVersionEvent.Metric(verifiedVersion, isCustom));

                    tcs.TrySetResult(hashSet);
                    return true;
                }

                return false;
            });
            return tcs.Task;
        }

        private UnityProjectKind GetProjectType()
        {
            if (myUnitySolutionTracker.IsUnityGeneratedProject.Value)
                return UnityProjectKind.Generated;
            else if (myUnitySolutionTracker.IsUnityProject.Value)
                return UnityProjectKind.Sidecar;
            else if (myUnitySolutionTracker.HasUnityReference.Value)
                return UnityProjectKind.Library;

            return UnityProjectKind.Other;
        }
    }
}