using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements.Prefabs;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements
{
    [PolymorphicMarshaller]
    public class ScriptComponentHierarchy : ComponentHierarchy, IScriptComponentHierarchy
    {
        [UsedImplicitly] 
        public new static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new ScriptComponentHierarchy(reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<ExternalReference>(),
            reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<LocalReference>(), reader.ReadPolymorphic<ExternalReference>(), reader.ReadBool());

        [UsedImplicitly]
        public new static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as ScriptComponentHierarchy);

        private static void Write(UnsafeWriter writer, ScriptComponentHierarchy value)
        {
            writer.WritePolymorphic(value.Location);
            writer.WritePolymorphic(value.ScriptReference);
            writer.WritePolymorphic(value.GameObjectReference);
            writer.WritePolymorphic(value.PrefabInstance);
            writer.WritePolymorphic(value.CorrespondingSourceObject);
            writer.Write(value.IsStripped);
        }
        
        public ScriptComponentHierarchy(LocalReference reference, ExternalReference scriptReference,
            LocalReference gameObject, LocalReference prefabInstance, ExternalReference correspondingSourceObject
            , bool isStripped) 
            : base("MonoBehaviour", reference, gameObject, prefabInstance, correspondingSourceObject, isStripped)
        {
            Assertion.Assert(isStripped || scriptReference != null, "isStripped || scriptReference != null");
            ScriptReference = scriptReference;
        }

        [CanBeNull]
        public virtual ExternalReference ScriptReference { get; }

        protected bool Equals(ScriptComponentHierarchy other)
        {
            return base.Equals(other) && Equals(ScriptReference, other.ScriptReference);
        }

        public override IHierarchyElement Import(IPrefabInstanceHierarchy prefabInstanceHierarchy)
        {
            return new ImportedScriptComponentHierarchy(prefabInstanceHierarchy, this);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScriptComponentHierarchy) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (ScriptReference != null ? ScriptReference.GetHashCode() : 0);
            }
        }
    }
}