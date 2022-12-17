using System.Linq;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class UnityEventUtils
    {
        public static bool TryGetDataIndex(string[] parts, out int index)
        {
            index = 0;
            var dataPart = parts.LastOrDefault(t => t.StartsWith("data"));
            if (dataPart == null)
                return false;

            if (!int.TryParse(dataPart.RemoveStart("data[").RemoveEnd("]"), out var i))
                return false;

            index = i;
            return true;
        }

        public static (string unityEventName, string[] parts) SplitPropertyPath(string modificationPropertyPath)
        {
            var splitPropertyPath = modificationPropertyPath.Split(".m_PersistentCalls.");
            var unityEventName = splitPropertyPath.First();
            var parts = splitPropertyPath.Last().Split('.');
            return (unityEventName, parts);
        }
    }
}