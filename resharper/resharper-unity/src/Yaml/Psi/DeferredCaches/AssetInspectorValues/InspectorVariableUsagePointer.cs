namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    public class InspectorVariableUsagePointer
    {
        public long OwnerId { get; }
        public int Index { get; }

        public InspectorVariableUsagePointer(long ownerId, int index)
        {
            OwnerId = ownerId;
            Index = index;
        }

        protected bool Equals(InspectorVariableUsagePointer other)
        {
            return OwnerId == other.OwnerId && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InspectorVariableUsagePointer) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (OwnerId.GetHashCode() * 397) ^ Index;
            }
        }
    }
}