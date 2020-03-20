namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues
{
    public class InspectorVariableUsagePointer
    {
        public int Index { get; }

        public InspectorVariableUsagePointer(int index)
        {
            Index = index;
        }

        protected bool Equals(InspectorVariableUsagePointer other)
        {
            return Index == other.Index;
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
            return Index;
        }
    }
}