namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages
{
    public readonly struct AssetUsagePointer
    {
        public long SourceFileIndex { get; }
        public int Index { get; }

        public AssetUsagePointer(long sourceFileIndex, int index)
        {
            SourceFileIndex = sourceFileIndex;
            Index = index;
        }

        public bool Equals(AssetUsagePointer other)
        {
            return SourceFileIndex == other.SourceFileIndex && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is AssetUsagePointer other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SourceFileIndex.GetHashCode() * 397) ^ Index;
            }
        }
    }
}