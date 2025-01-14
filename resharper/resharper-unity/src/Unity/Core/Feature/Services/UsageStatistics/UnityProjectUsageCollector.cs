using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Application.Parts;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.FeaturesStatistics;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.Technologies
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityProjectUsageCollector : SolutionUsagesCollector
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly UnityTechnologyDescriptionCollector myDescriptionCollector;
        private EventLogGroup myGroup;
        private readonly EventId2<string, bool> myEvent;

        public UnityProjectUsageCollector(UnitySolutionTracker unitySolutionTracker, UnityTechnologyDescriptionCollector descriptionCollector, FeatureUsageLogger featureUsageLogger)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myDescriptionCollector = descriptionCollector;
            myGroup = new EventLogGroup("dotnet.unity.state", "Packages and Assets used by Unity users", 5, featureUsageLogger);
            myEvent = myGroup.RegisterEvent("unityTechnology", "Unity Technology", EventFields.String("id", "Name",
                new[] { "HDRP", "CoreRP", "URP", "ECS", "InputSystem", "Burst", "Odin", "Photon", "Peek", "UniRx",
                    "UniTask",
                    "DOTween",
                    "TestFramework",
                    "PythonScripting",
                    "Addressables",
                    "AndroidLogCat",
                    "CodeCoverage",
                    "UnityCollections",
                    "EditorCoroutines",
                    "EntitiesGraphics",
                    "Localization",
                    "Mathematics",
                    "Transport",
                    "UnityPhysics",
                    "Havok",
                    "MlAgents",
                    "MultiplayerTools",
                    "NetCode",
                    "NetCodeGameObjects",
                    "Serialization",
                    "Logging",
                    "MemoryProfiler",
                    "ProfilerAnalyzer",
                    "ProfilerCore",
                    "Collab",
                    "VisualScripting",
                    "HotReload",
                    "UnityNoesis"
                }), EventFields.Boolean("isDiscovered", "Is Discovered"));
        }
        
        public override EventLogGroup GetGroup()
        {
            return myGroup;
        }


        public override async Task<ISet<MetricEvent>> GetMetricsAsync(Lifetime lifetime)
        {
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return EmptySet<MetricEvent>.Instance;
            
            return await lifetime.StartMainReadAsync(async () =>
            {
                await myDescriptionCollector.Ready.NextTrueValueAsync(lifetime);
                var hashSet = new HashSet<MetricEvent>();
                foreach (var (key, value) in myDescriptionCollector.DiscoveredTechnologies)
                {
                    hashSet.Add(myEvent.Metric(key, value));
                }

                return hashSet;
            });
        }
    }
}