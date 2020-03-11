using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    public class AssetUsage
    {
        public LocalReference Location { get; }
        public List<IHierarchyReference> Dependencies { get; }

        public AssetUsage(LocalReference location, IEnumerable<IHierarchyReference> dependencies)
        {
            Location = location;
            Dependencies = dependencies.ToList();
        }

        protected bool Equals(AssetUsage other)
        {
            return Location.Equals(other.Location);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetUsage) obj);
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode();
        }

        public void WriteTo(UnsafeWriter writer)
        {
            writer.WritePolymorphic(Location);
            writer.Write(Dependencies.Count);
            foreach (var dependency in Dependencies)
            {
                writer.WritePolymorphic(dependency);
            }
        }

        public static AssetUsage ReadFrom(UnsafeReader reader)
        {
            var localReference = reader.ReadPolymorphic<LocalReference>();
            var count = reader.ReadInt32();
            var deps = new List<IHierarchyReference>();
            for (int i = 0; i < count; i++)
                deps.Add(reader.ReadPolymorphic<IHierarchyReference>());
            return new AssetUsage(localReference, deps);
        }
        
        
    }
}