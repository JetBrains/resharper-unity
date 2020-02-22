using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class AssetReferenceValue : IAssetValue
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new AssetReferenceValue( reader.ReadPolymorphic<IHierarchyReference>());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetReferenceValue);

        private static void Write(UnsafeWriter writer, AssetReferenceValue value)
        {
            writer.WritePolymorphic(value.Reference);
        }
        
        public IHierarchyReference Reference { get; }
        
        public AssetReferenceValue(IHierarchyReference reference)
        {
            Reference = reference;
        }

        protected bool Equals(AssetReferenceValue other)
        {
            return Reference.Equals(other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetReferenceValue) obj);
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }

        public string GetPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport)
        {
            solution.GetComponent<IShellLocks>().AssertReadAccessAllowed();

            var processor = solution.GetComponent<AssetHierarchyProcessor>();
            var consumer = new UnityScenePathGameObjectConsumer(true);
            var hierarchyContainer = solution.GetComponent<AssetDocumentHierarchyElementContainer>();
            var element = hierarchyContainer.GetHierarchyElement(Reference, prefabImport);
            if (element == null)
                return "...";
            processor.ProcessSceneHierarchyFromComponentToRoot(element, consumer, prefabImport);
            if (consumer.NameParts.Count == 0)
                return "...";
            var result =  string.Join("/", consumer.NameParts);

            if (element is IComponentHierarchy componentHierarchy)
                result += $" ({AssetUtils.GetComponentName(solution.GetComponent<MetaFileGuidCache>(), componentHierarchy)})";

            return result;
        }
    }
}