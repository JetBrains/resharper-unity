using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetUsages;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityScriptsFindResults : UnityAssetFindResult
    {
        public AssetUsage AssetUsage { get; }

        public UnityScriptsFindResults(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, AssetUsage assetUsage, IHierarchyElement attachedElement)
            : base(sourceFile, declaredElement, attachedElement)
        {
            AssetUsage = assetUsage;
        }

        protected bool Equals(UnityScriptsFindResults other)
        {
            return base.Equals(other) && AssetUsage.Equals(other.AssetUsage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityScriptsFindResults) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ AssetUsage.GetHashCode();
            }
        }
    }
}