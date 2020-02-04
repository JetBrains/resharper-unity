using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class ScriptComponentHierarchy : ComponentHierarchy
    {
        [UsedImplicitly] 
        public new static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new ScriptComponentHierarchy(reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<ExternalReference>());

        [UsedImplicitly]
        public new static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ScriptComponentHierarchy);

        private static void Write(UnsafeWriter writer, ScriptComponentHierarchy value)
        {
            writer.WritePolymorphic(value.LocalReference);
            writer.WritePolymorphic(value.ScriptReference);
        }
        
        public ScriptComponentHierarchy(LocalReference reference, ExternalReference scriptReference) : base(reference)
        {
            ScriptReference = scriptReference;
        }

        public ExternalReference ScriptReference { get; }
    }
}