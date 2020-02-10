using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values
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

        public string GetPresentation(ISolution solution, IPersistentIndexManager persistentIndexManager,
            AssetDocumentHierarchyElementContainer assetDocument, IType type)
        {
            return "Cube (test)";
        }
    }
}