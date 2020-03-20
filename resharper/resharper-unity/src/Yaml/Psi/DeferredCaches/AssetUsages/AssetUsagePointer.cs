namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    public class AssetUsagePointer
    {
        public int Index { get; }

        public AssetUsagePointer(int index)
        {
            Index = index;
        }

        protected bool Equals(AssetUsagePointer other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetUsagePointer) obj);
        }

        public override int GetHashCode()
        {
            return Index;
        }
    }
}