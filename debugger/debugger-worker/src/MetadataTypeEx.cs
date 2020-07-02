using JetBrains.Annotations;
using MetadataLite.API;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
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