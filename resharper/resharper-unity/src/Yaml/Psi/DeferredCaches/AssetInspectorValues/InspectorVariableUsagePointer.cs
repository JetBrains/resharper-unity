namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    public readonly struct InspectorVariableUsagePointer
    {
        public long OwnerId { get; }
        public int Index { get; }

        public InspectorVariableUsagePointer(long ownerId, int index)
        {
            OwnerId = ownerId;
            Index = index;
        }

        public bool Equals(InspectorVariableUsagePointer other)
        {
            return OwnerId == other.OwnerId && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is InspectorVariableUsagePointer other && Equals(other);
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