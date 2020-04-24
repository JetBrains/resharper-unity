namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References
{
    public struct NullReference : IHierarchyReference
    {
        public ulong LocalDocumentAnchor => 0;

        public override bool Equals(object obj)
        {
            if (obj is NullReference)
                return true;
            
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }
}