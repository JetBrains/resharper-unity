using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity
{
    // Cache version
    [PolymorphicMarshaller(20)]
    public class CacheVersion
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = r => new CacheVersion();
        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => { };
    }
}