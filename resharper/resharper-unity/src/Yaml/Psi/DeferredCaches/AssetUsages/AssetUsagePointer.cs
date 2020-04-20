namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    public class AssetUsagePointer
    {
        public long SourceFileIndex { get; }
        public int Index { get; }

        public AssetUsagePointer(long sourceFileIndex, int index)
        {
            SourceFileIndex = sourceFileIndex;
            Index = index;
        }

        protected bool Equals(AssetUsagePointer other)
        {
            return Index == other.Index && SourceFileIndex == other.SourceFileIndex;
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
            unchecked
            {
                return (Index * 397) ^ SourceFileIndex.GetHashCode();
            }
        }
    }
}