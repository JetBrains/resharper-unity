using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    public readonly struct AssetUsage
    {
        // TODO, local reference deps
        public LocalReference Location { get; }
        public ExternalReference ExternalDependency { get; }

        public AssetUsage(LocalReference location, ExternalReference externalDependency)
        {
            Location = location;
            ExternalDependency = externalDependency;
        }

        public bool Equals(AssetUsage other)
        {
            return Location.Equals(other.Location) && ExternalDependency.Equals(other.ExternalDependency);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetUsage other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Location.GetHashCode() * 397) ^ ExternalDependency.GetHashCode();
            }
        }

        public void WriteTo(UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            ExternalDependency.WriteTo(writer);
        }

        public static AssetUsage ReadFrom(UnsafeReader reader)
        {
            var localReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            return new AssetUsage(localReference, HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
        
    }
}