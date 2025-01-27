#nullable enable
using JetBrains.UsageStatistics.FUS.Collectors;
using JetBrains.UsageStatistics.FUS.EventLog;
using JetBrains.UsageStatistics.FUS.EventLog.Events;
using JetBrains.UsageStatistics.FUS.EventLog.Fus;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Profiler;

[CounterUsagesCollector]
public class UnityProfilerInfoCollector : CounterUsagesCollector
{
    private readonly EventLogGroup myGroup;
    private readonly EventId myNavigateFromProfiler;
    private readonly EventId1<int> myProfilerFrameSelected;
    private readonly EventId1<int> mySnapshotFetched;

    public UnityProfilerInfoCollector(FeatureUsageLogger featureUsageLogger)
    {
        myGroup = new EventLogGroup("dotnet.unity.profiler", "Unity Profiler Integration Information", 1, featureUsageLogger);
        
        myNavigateFromProfiler = myGroup.RegisterEvent("navigated_from_unity_profiler",
            "User navigated from Unity Profiler window to the code");

        myProfilerFrameSelected = myGroup.RegisterEvent("selected_frame_in_unity_profiler",
            "User selected frame in Unity Profiler window",
            EventFields.RoundedInt("samples_count", "Number of samples in selected frame"));

        mySnapshotFetched = myGroup.RegisterEvent("snapshot_fetched",
            "Unity profiler snapshot fetched from the editor to Rider",
            EventFields.RoundedInt("samples_count", "Number of samples in fetched snapshot"));
    }

    public override EventLogGroup GetGroup() => myGroup;

    public void OnSnapshotFetched(int samplesCount)
    {
        mySnapshotFetched.Log(samplesCount);
    }

    public void OnOpenFileBySampleInfo()
    {
        myNavigateFromProfiler.Log();
    }

    public void OnUnityProfilerFrameSelected(int numberOfSamples)
    {
        myProfilerFrameSelected.Log(numberOfSamples);
    }
}