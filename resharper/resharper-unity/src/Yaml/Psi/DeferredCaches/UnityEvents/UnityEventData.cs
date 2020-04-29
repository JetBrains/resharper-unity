using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents
{
    public class UnityEventData
    {
        public string Name { get; }
        public LocalReference Location { get; }
        public ExternalReference ScriptReference { get; }
        public IReadOnlyList<AssetMethodData> Calls { get; }

        public UnityEventData(string name, LocalReference location, ExternalReference scriptReference,
            IEnumerable<AssetMethodData> calls)
        {
            Name = name;
            Location = location;
            ScriptReference = scriptReference;
            Calls = calls.ToList();
        }
        
        public static UnityEventData ReadFrom(UnsafeReader reader)
        {
            var name = reader.ReadString();
            var location = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            var scriptReference = HierarchyReferenceUtil.ReadExternalReferenceFrom(reader);
            var count = reader.ReadInt();
            var calls = new List<AssetMethodData>();
            for (int i = 0; i < count; i++)
            {
                calls.Add(AssetMethodData.ReadFrom(reader));
            }
            
            return new UnityEventData(name, location, scriptReference, calls);
        }

        public void WriteTo(UnsafeWriter writer)
        {
            writer.Write(Name);
            Location.WriteTo(writer);
            ScriptReference.WriteTo(writer);
            writer.Write(Calls.Count);
            foreach (var call in Calls)
            {
                call.WriteTo(writer);
            }
        }
    }
}