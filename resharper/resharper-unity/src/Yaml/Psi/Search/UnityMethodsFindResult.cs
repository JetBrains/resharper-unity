using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetMethods;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityMethodsFindResult : UnityAssetFindResult
    {
        public AssetMethodData AssetMethodData { get; }

        public UnityMethodsFindResult(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, AssetMethodData assetMethodData, IHierarchyElement attachedElement)
            : base(sourceFile, declaredElement, attachedElement)
        {
            AssetMethodData = assetMethodData;
        }

        protected bool Equals(UnityMethodsFindResult other)
        {
            return base.Equals(other) && AssetMethodData.Equals(other.AssetMethodData);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityMethodsFindResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ AssetMethodData.GetHashCode();
            }
        }
    }
}