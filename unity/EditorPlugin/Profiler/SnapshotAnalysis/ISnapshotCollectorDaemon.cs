#nullable enable
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.BackendUnity;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis
{
  public interface ISnapshotCollectorDaemon
  {
    void Update(EditorWindow? ourLastProfilerWindow);
    void Deinit();
    void Advise(Lifetime connectionLifetime, UnityProfilerModel model);
  }
}