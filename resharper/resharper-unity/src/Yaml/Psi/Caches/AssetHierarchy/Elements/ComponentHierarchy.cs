using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class ComponentHierarchy : IHierarchyElement
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new ComponentHierarchy(reader.ReadPolymorphic<LocalReference>());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ComponentHierarchy);

        private static void Write(UnsafeWriter writer, ComponentHierarchy value)
        {
            writer.WritePolymorphic(value.LocalReference);
        }
        
        public ComponentHierarchy(LocalReference localReference)
        {
            LocalReference = localReference;
        }

        public LocalReference LocalReference { get; }
    }
}