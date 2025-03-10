#nullable enable
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation
{
  public delegate void OnTimeSampleSelected(string sampleName, string callStack);

  public interface IProfilerWindowSelectionDataProvider
  {
    bool IsInitialized { get; }
    bool IsSupportingCurrentUnityVersion { get; }
    void Deinit(EditorWindow? profilerWindow, OnTimeSampleSelected? onTimeSampleSelected);
    void Init(EditorWindow profilerWindow, OnTimeSampleSelected? onTimeSampleSelected);
  }
}