using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Services.FeatureStatistics
{
    [SolutionComponent]
    public class UnityProjectUsageCollector : SolutionUsagesCollector
    {
        private readonly UnityTechnologyDescriptionCollector myDescriptionCollector;
        private EventLogGroup myGroup;
        private readonly EventId2<string, bool> myEvent;

        public UnityProjectUsageCollector(UnityTechnologyDescriptionCollector descriptionCollector, FeatureUsageLogger featureUsageLogger)
        {
            myDescriptionCollector = descriptionCollector;
            myGroup = new EventLogGroup("dotnet.unity.state", "Packages and Assets used by Unity users", 1, featureUsageLogger);
            myEvent = myGroup.RegisterEvent("unityTechnology", "Unity Technology", EventFields.String("id", "Name",
                new[] { "HDRP", "CoreRP", "URP", "ECS", "InputSystem", "Burst", "Odin", "Peek", "UniRx", "UniTask" }), EventFields.Boolean("isDiscovered", "Is Discovered"));
        }
        
        public override EventLogGroup GetGroup()
        {
            return myGroup;
        }


        public override Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
        {
            var tcs = lifetime.CreateTaskCompletionSource<ISet<MetricEvent>>(TaskCreationOptions.RunContinuationsAsynchronously);

            myDescriptionCollector.Ready.AdviseUntil(lifetime, v =>
            {
                if (v)
                {
                    var hashSet = new HashSet<MetricEvent>();
                    foreach (var (key, value) in myDescriptionCollector.DiscoveredTechnologies)
                    {
                        hashSet.Add(myEvent.Metric(key, value));
                    }

                    tcs.TrySetResult(hashSet);
                    return true;
                }

                return false;
            });
            return tcs.Task;
        }
    }
}