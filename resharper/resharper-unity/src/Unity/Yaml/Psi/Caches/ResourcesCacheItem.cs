using JetBrains.Annotations;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public enum ResourceLocationType
    {
        Player,
        Editor,
        PackagePlayer,
        PackageEditor,
    }

    public class ResourcesCacheItem
    {
        public static readonly IUnsafeMarshaller<ResourcesCacheItem> Marshaller =
            new UniversalMarshaller<ResourcesCacheItem>(Read, Write);

        private readonly ResourceLocationType myLocationType;
        private readonly VirtualFileSystemPath myPathInsideResourcesFolder;
        [CanBeNull] private readonly RelativePath myRelativePath;
        private readonly string myExtensionWithDot;

        public ResourcesCacheItem(ResourceLocationType locationType, VirtualFileSystemPath pathInsideResourcesFolder,
            RelativePath relativePath, string extensionWithDot)
        {
            myLocationType = locationType;
            myPathInsideResourcesFolder = pathInsideResourcesFolder;
            myRelativePath = relativePath;
            myExtensionWithDot = extensionWithDot;
        }

        public ResourceLocationType LocationType => myLocationType;
        public VirtualFileSystemPath PathInsideResourcesFolder => myPathInsideResourcesFolder;

        [CanBeNull] public RelativePath RelativePath => myRelativePath;

        public string ExtensionWithDot => myExtensionWithDot;

        private static void Write(UnsafeWriter writer, ResourcesCacheItem value)
        {
            writer.WriteEnum(value.myLocationType);
            writer.Write(value.myPathInsideResourcesFolder);
            writer.Write(value.myRelativePath);
            writer.Write(value.myExtensionWithDot);
        }

        private static ResourcesCacheItem Read(UnsafeReader reader)
        {
            var locationType = reader.ReadEnum<ResourceLocationType>();
            var pathInsideResourcesDirectory = reader.ReadCurrentSolutionVirtualFileSystemPath();
            var relativePath = reader.ReadRelativePath();
            var extensionWithDot = reader.ReadString();
            
            return new ResourcesCacheItem(locationType.Value, pathInsideResourcesDirectory, relativePath, extensionWithDot);    
        }

        public override string ToString()
        {
            return $"{nameof(LocationType)}: {LocationType}, {nameof(PathInsideResourcesFolder)}: {PathInsideResourcesFolder}, {nameof(RelativePath)}: {RelativePath}, {nameof(ExtensionWithDot)}: {ExtensionWithDot}";
        }
    }
}