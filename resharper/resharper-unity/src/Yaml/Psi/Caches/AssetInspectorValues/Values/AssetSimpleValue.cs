using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Serialization;
using static JetBrains.Serialization.UnsafeWriter;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class AssetSimpleValue : IAssetValue
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new AssetSimpleValue(reader.ReadString());

        [UsedImplicitly]
        public static WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetSimpleValue);

        private static void Write(UnsafeWriter writer, AssetSimpleValue value)
        {
            writer.Write(value.SimpleValue);
        }

        public AssetSimpleValue(string value)
        {
            SimpleValue = value ?? string.Empty;
        }

        protected bool Equals(AssetSimpleValue other)
        {
            return SimpleValue == other.SimpleValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetSimpleValue) obj);
        }

        public override int GetHashCode()
        {
            return SimpleValue.GetHashCode();
        }

        public string GetPresentation(ISolution solution, IPersistentIndexManager persistentIndexManager,
            AssetDocumentHierarchyElementContainer assetDocument, IType type)
        {
            return SimpleValue;
        }

        public string SimpleValue { get; }
    }
}