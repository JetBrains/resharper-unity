using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class UnityEventsBuildResult
    {
        public UnityEventsBuildResult(ImportedUnityEventData modificationDescription, LocalList<UnityEventData> unityEventData)
        {
            ModificationDescription = modificationDescription;
            UnityEventData = unityEventData;
        }

        public LocalList<UnityEventData> UnityEventData { get; }
        public ImportedUnityEventData ModificationDescription { get; }
    }
}