using JetBrains.Annotations;
using Mono.Debugging.MetadataLite.API;

namespace JetBrains.Debugger.Worker.Plugins.Unity
{
    // TODO move into platform
    public static class MetadataTypeEx
    {
        [CanBeNull]
        public static IMetadataTypeLite FindTypeThroughHierarchy([NotNull] this IMetadataTypeLite metadataType, string fullClrName)
        {
            var current = metadataType;
            while (current != null)
            {
                if (current.Is(fullClrName))
                    return current;
                current = current.BaseType;
            }

            return null;
        }
    }
}