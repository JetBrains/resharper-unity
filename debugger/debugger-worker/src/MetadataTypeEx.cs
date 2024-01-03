using Mono.Debugging.MetadataLite.API;

namespace JetBrains.Debugger.Worker.Plugins.Unity
{
    // TODO move into platform
    public static class MetadataTypeEx
    {
        public static IMetadataTypeLite? FindTypeThroughHierarchy(this IMetadataTypeLite metadataType, string fullClrName)
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