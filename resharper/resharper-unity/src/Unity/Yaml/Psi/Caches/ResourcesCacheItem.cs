using JetBrains.Annotations;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    internal enum ResourceLocationType
    {
        Player,
        Editor,
        PackagePlayer,
        PackageEditor,
    }

    internal class ResourcesCacheItem
    {
        public static readonly IUnsafeMarshaller<ResourcesCacheItem> Marshaller =
            new UniversalMarshaller<ResourcesCacheItem>(Read, Write);

        public ResourcesCacheItem(ResourceLocationType locationType, VirtualFileSystemPath pathInsideResourcesFolder,
            RelativePath relativePath, string extensionWithDot)
        {
            LocationType = locationType;
            PathInsideResourcesFolder = pathInsideResourcesFolder;
            RelativePath = relativePath;
            ExtensionWithDot = extensionWithDot;
        }

        public ResourceLocationType LocationType { get; }

        public VirtualFileSystemPath PathInsideResourcesFolder { get; }

        [CanBeNull] public RelativePath RelativePath { get; }

        public string ExtensionWithDot { get; }
        
        private static void Write(UnsafeWriter writer, ResourcesCacheItem value)
        {
            writer.WriteEnum(value.LocationType);
            writer.Write(value.PathInsideResourcesFolder);
            writer.Write(value.RelativePath);
            writer.Write(value.ExtensionWithDot);
        }

        private static ResourcesCacheItem Read(UnsafeReader reader)
        {
            var locationType = reader.ReadEnum<ResourceLocationType>();
            var pathInsideResourcesDirectory = reader.ReadCurrentSolutionVirtualFileSystemPath();
            var relativePath = reader.ReadRelativePath();
            var extensionWithDot = reader.ReadString();
            
            return new ResourcesCacheItem(locationType!.Value, pathInsideResourcesDirectory, relativePath, extensionWithDot);    
        }

        public override string ToString()
        {
            return $"{nameof(LocationType)}: {LocationType}, {nameof(PathInsideResourcesFolder)}: {PathInsideResourcesFolder}, {nameof(RelativePath)}: {RelativePath}, {nameof(ExtensionWithDot)}: {ExtensionWithDot}";
        }
    }
}