using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Feature.Services.Project;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Services.FeatureStatistics
{
    [SolutionComponent]
    public class UnityProjectInformationUsageCollector : SolutionUsagesCollector
    {
        private readonly ISolution mySolution;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly FeaturesStartupMonitor myMonitor;
        private readonly ILogger myLogger;

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
        private readonly EventId3<bool, bool, int> myEnterPlayModeOptionsEvent;
        private readonly UnityExternalFilesPsiModule myUnityModule;

        public UnityProjectInformationUsageCollector(ISolution solution, UnitySolutionTracker unitySolutionTracker, FeaturesStartupMonitor monitor, FeatureUsageLogger featureUsageLogger, ILogger logger)
        {
            mySolution = solution;
            myUnitySolutionTracker = unitySolutionTracker;
            myMonitor = monitor;
            myLogger = logger;
            myUnityModule = UnityProjectSettingsUtils.GetUnityModule(solution);
            myGroup = new EventLogGroup("dotnet.unity.projects", "Unity Project Information", 1, featureUsageLogger);
            myProjectKindEvent = myGroup.RegisterEvent("projectKind", "Project Kind", 
                EventFields.Enum<UnityProjectKind>("type", "Type"));
            
            myUnityVersionEvent = myGroup.RegisterEvent("version", "Project Unity Version", 
                EventFields.StringValidatedByRegexp("version", "Unity Version", UnityVersion.VersionRegex),
                EventFields.Boolean("isCustom", "Custom Unity Build")); 
            myEnterPlayModeOptionsEvent = myGroup.RegisterEvent("EnterPlayModeOptions", "Enter Play Mode Options",
                EventFields.Boolean("Exists", "EnterPlayModeOptionsEnabled exists in the project"),
                EventFields.Boolean("EnterPlayModeOptionsEnabled", "Enter Play Mode Option"),
                EventFields.Int("EnterPlayModeOptions", "EnterPlayModeOptions flags"));
        }
        
        public override EventLogGroup GetGroup()
        {
            return myGroup;
        }

        public static (string, bool) GetUnityVersion(string versionInfo)
        {
            const string unknownVersion = "0.0.0f0";
            versionInfo = versionInfo ?? unknownVersion;
            var match = Regex.Match(versionInfo, UnityVersion.VersionRegex);
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
                    var (exists, isEnabled, options) = GetEnterPlayModeOptions();
                    hashSet.Add(myEnterPlayModeOptionsEvent.Metric(exists, isEnabled, options));

                    tcs.TrySetResult(hashSet);
                    return true;
                }

                return false;
            });
            return tcs.Task;
        }

        private (bool exists, bool isEnabled, int options) GetEnterPlayModeOptions()
        {
            var editorSettings = UnityProjectSettingsUtils.GetEditorSettings(myUnityModule);
            Assertion.Assert(editorSettings != null);
            IYamlFile yamlFile;
            using (ReadLockCookie.Create())
            {
                yamlFile = editorSettings.GetDominantPsiFile<YamlLanguage>() as IYamlFile;    
            }
            Assertion.Assert(yamlFile != null);
            var node = UnityProjectSettingsUtils.GetValue<INode>(yamlFile, "EditorSettings",
                "m_EnterPlayModeOptionsEnabled");
            var optionsNode = UnityProjectSettingsUtils.GetValue<INode>(yamlFile, "EditorSettings", "m_EnterPlayModeOptions");
            if (node == null)
                return (false, false, 0);
            if (optionsNode == null)
            {
                myLogger.Error("m_EnterPlayModeOptions was not found, but m_EnterPlayModeOptionsEnabled was present.");
                return (false, false, 0);
            }
            return (true, Convert.ToBoolean(Convert.ToInt32(node.GetText())), 
               Convert.ToInt32(optionsNode.GetText()));
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