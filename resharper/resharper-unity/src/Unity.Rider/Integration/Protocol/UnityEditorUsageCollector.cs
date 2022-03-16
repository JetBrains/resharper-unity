using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Services.FeatureStatistics;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol
{
    [SolutionComponent]
    public class UnityEditorUsageCollector : SolutionUsagesCollector
    {
        private enum ScriptingRuntime
        {
            Net35,
            Net46
        }
        
        private EventLogGroup myGroup;
        
        private readonly EventId1<ScriptingRuntime> myScriptingRuntimeEvent;
        private readonly EventId2<string, bool> myUnityVersionEvent;

        private IViewableProperty<bool> IsReady { get; } = new ViewableProperty<bool>(false);

        public UnityEditorUsageCollector(FeatureUsageLogger featureUsageLogger)
        {
            myGroup = new EventLogGroup("dotnet.unity.unityeditor", "Connected Unity Editor Information", 1, featureUsageLogger);
            
            myScriptingRuntimeEvent = myGroup.RegisterEvent("scriptingRuntime", "Unity Scripting Runtime",
                EventFields.Enum<ScriptingRuntime>("scriptingRuntime", "Scripting Runtime"));
            
            myUnityVersionEvent = myGroup.RegisterEvent("unityVersion", "Project Unity Version", 
                EventFields.StringValidatedByRegexp("unityVersion", "Unity Version", UnityProjectInformationUsageCollector.VersionRegex),
                EventFields.Boolean("isCustomUnityVersion", "Custom Unity Build")); 
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
            
            (myUnityVersion, myUnityVersionCustom) = UnityProjectInformationUsageCollector.GetUnityVersion(unityVersion);

            IsReady.Value = true;
        }

        public override Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
        {
            var tcs = lifetime.CreateTaskCompletionSource<ISet<MetricEvent>>(TaskCreationOptions.RunContinuationsAsynchronously);

            IsReady.AdviseUntil(lifetime, v =>
            {
                if (v)
                {
                    var hashSet = new HashSet<MetricEvent>();
                    hashSet.Add(myUnityVersionEvent.Metric(myUnityVersion, myUnityVersionCustom));
                    hashSet.Add(myScriptingRuntimeEvent.Metric(myScriptingRuntime));
                    tcs.TrySetResult(hashSet);
                    return true;
                }

                return false;
            });
            
            return tcs.Task;
        }
    }
}