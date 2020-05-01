using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    [PolymorphicMarshaller]
    public class InspectorVariableUsage
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new InspectorVariableUsage(
            HierarchyReferenceUtil.ReadLocalReferenceFrom(reader),
            HierarchyReferenceUtil.ReadExternalReferenceFrom(reader),
            reader.ReadString(), 
            reader.ReadPolymorphic<IAssetValue>());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as InspectorVariableUsage);

        private static void Write(UnsafeWriter writer, InspectorVariableUsage value)
        {
            value.Location.WriteTo(writer);
            value.ScriptReference.WriteTo(writer);
            writer.Write(value.Name);
            writer.WritePolymorphic(value.Value);
        }

        public InspectorVariableUsage(LocalReference locationIndex, ExternalReference scriptReferenceIndex, string name,
            IAssetValue assetValue)
        {
            Assertion.Assert(name != null, "name != null");
            Location = locationIndex;
            ScriptReference = scriptReferenceIndex;
            Name = string.Intern(name);
            Value = assetValue;
        }
        
        public LocalReference Location { get; }
        public ExternalReference ScriptReference { get; }
        public string Name { get; }
        public IAssetValue Value { get; }

        protected bool Equals(InspectorVariableUsage other)
        {
            return Location.Equals(other.Location) && ScriptReference.Equals(other.ScriptReference) && Name == other.Name && Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InspectorVariableUsage) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Location.GetHashCode();
                hashCode = (hashCode * 397) ^ ScriptReference.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                return hashCode;
            }
        }
    }
}