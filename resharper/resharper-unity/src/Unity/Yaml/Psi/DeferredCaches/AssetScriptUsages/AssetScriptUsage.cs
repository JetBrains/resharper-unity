using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetScriptUsages
{
    // TODO : Right now, we use it only for scripts, but we could calculate deps not only for scripts
    public readonly struct AssetScriptUsage : IScriptUsage
    {
        // TODO, local reference deps
        public LocalReference Location { get; }
        public ExternalReference UsageTarget { get; }

        public AssetScriptUsage(LocalReference location, ExternalReference usageTarget)
        {
            Location = location;
            UsageTarget = usageTarget;
        }

        public bool Equals(AssetScriptUsage other)
        {
            return Location.Equals(other.Location) && UsageTarget.Equals(other.UsageTarget);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetScriptUsage other && Equals(other);
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

        public static AssetScriptUsage ReadFrom(UnsafeReader reader)
        {
            var localReference = HierarchyReferenceUtil.ReadLocalReferenceFrom(reader);
            return new AssetScriptUsage(localReference, HierarchyReferenceUtil.ReadExternalReferenceFrom(reader));
        }
        
    }
}