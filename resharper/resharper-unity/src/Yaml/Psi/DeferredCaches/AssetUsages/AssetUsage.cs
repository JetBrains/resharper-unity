using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    public class AssetUsage
    {
        // TODO, local reference deps
        public LocalReference Location { get; }
        public ExternalReference ExternalDependency { get; }

        public AssetUsage(LocalReference location, ExternalReference externalDependency)
        {
            Location = location;
            ExternalDependency = externalDependency;
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
            ExternalDependency.WriteTo(writer);
        }

        public static AssetUsage ReadFrom(UnsafeReader reader)
        {
            var localReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            return new AssetUsage(localReference, HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
        
    }
}