using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol
{
    [SolutionComponent]
    public class UnityEditorUsageCollector : SolutionUsagesCollector
    {
        private readonly UnitySolutionTracker mySolutionTracker;

        private enum ScriptingRuntime
        {
            Net35,
            Net46
        }
        
        private EventLogGroup myGroup;
        
        private readonly EventId3<string, bool, ScriptingRuntime> myConnectedUnityEvent;

        private IViewableProperty<bool> IsReady { get; } = new ViewableProperty<bool>(false);

        public UnityEditorUsageCollector(UnitySolutionTracker solutionTracker, FeatureUsageLogger featureUsageLogger)
        {
            mySolutionTracker = solutionTracker;
            myGroup = new EventLogGroup("dotnet.unity.unityeditor", "Connected Unity Editor Information", 1, featureUsageLogger);
            
            myConnectedUnityEvent = myGroup.RegisterEvent("version", "Project Unity Version", 
                EventFields.StringValidatedByRegexp("version", "Unity Version", UnityVersion.VersionRegex),
                EventFields.Boolean("isCustom", "Custom Unity Build"),
                EventFields.Enum<ScriptingRuntime>("scriptingRuntime", "Scripting Runtime")); 
        }
        
        public override EventLogGroup GetGroup()
        {
            return myGroup;
        }

        private string myUnityVersion;
        private bool myUnityVersionCustom;
        private ScriptingRuntime myScriptingRuntime;
        
        public void SetInformation(string unityVersion, int scriptingRuntime)
        {
            if (IsReady.Value)
                return;
            
            if (scriptingRuntime == 0)
                myScriptingRuntime = ScriptingRuntime.Net35;
            else
                myScriptingRuntime = ScriptingRuntime.Net46;
            
            (myUnityVersion, myUnityVersionCustom) = UnityVersionUtils.GetUnityVersion(unityVersion);

            IsReady.Value = true;
        }

        public override async Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
        {
            if (!mySolutionTracker.IsUnityProject.HasTrueValue())
                return EmptySet<MetricEvent>.Instance;
                
            return await lifetime.StartMainReadAsync(async () =>
            {
                await IsReady.NextTrueValueAsync(lifetime);
                var hashSet = new HashSet<MetricEvent>
                {
                    myConnectedUnityEvent.Metric(myUnityVersion, myUnityVersionCustom, myScriptingRuntime)
                };
                
                return hashSet;
            });
        }
    }
}