using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class UnityEventData
    {
        public string Name { get; }
        public LocalReference OwningScriptLocation { get; }
        public ExternalReference ScriptReference { get; }
        public IReadOnlyList<AssetMethodUsages> Calls { get; }

        public UnityEventData(string name, LocalReference owningScriptLocation, ExternalReference scriptReference,
            IEnumerable<AssetMethodUsages> calls)
        {
            Name = name;
            OwningScriptLocation = owningScriptLocation;
            ScriptReference = scriptReference;
            Calls = calls.ToList();
        }
        
        public static UnityEventData ReadFrom(UnsafeReader reader)
        {
            var name = reader.ReadString();
            var location = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var scriptReference = HierarchyReferenceUtil.ReadExternalReferenceFrom(reader);
            var count = reader.ReadInt();
            var calls = new List<AssetMethodUsages>();
            for (int i = 0; i < count; i++)
            {
                calls.Add(AssetMethodUsages.ReadFrom(reader));
            }
            
            return new UnityEventData(name, location, scriptReference, calls);
        }

        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(Name);
            OwningScriptLocation.WriteTo(writer);
            ScriptReference.WriteTo(writer);
            writer.Write(Calls.Count);
            foreach (var call in Calls)
            {
                call.WriteTo(writer);
            }
        }
    }
}