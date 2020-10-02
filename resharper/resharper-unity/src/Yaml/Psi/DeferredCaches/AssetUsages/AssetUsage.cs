using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.PersistentMap;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    // TODO : Right now, we use it only for scripts, but we could calculate deps not only for scripts
    public readonly struct AssetScriptUsages : IScriptUsage
    {
        // TODO, local reference deps
        public LocalReference Location { get; }
        public ExternalReference UsageTarget { get; }

        public AssetScriptUsages(LocalReference location, ExternalReference usageTarget)
        {
            Location = location;
            UsageTarget = usageTarget;
        }

        public bool Equals(AssetScriptUsages other)
        {
            return Location.Equals(other.Location) && UsageTarget.Equals(other.UsageTarget);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetScriptUsages other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Location.GetHashCode() * 397) ^ UsageTarget.GetHashCode();
            }
        }

        public void WriteTo(UnsafeWriter writer)
        {
            Location.WriteTo(writer);
            UsageTarget.WriteTo(writer);
        }

        public static AssetScriptUsages ReadFrom(UnsafeReader reader)
        {
            var localReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            return new AssetScriptUsages(localReference, HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
        
    }
}