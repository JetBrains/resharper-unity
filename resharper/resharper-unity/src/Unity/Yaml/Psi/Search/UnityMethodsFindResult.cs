using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityEventHandlerFindResult : UnityAssetFindResult
    {
        public AssetMethodUsages AssetMethodUsages { get; }
        public bool IsPrefabModification { get; }

        public UnityEventHandlerFindResult(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, AssetMethodUsages assetMethodUsages,
            LocalReference owningElementLocation, bool isPrefabModification)
            : base(sourceFile, declaredElement, owningElementLocation)
        {
            AssetMethodUsages = assetMethodUsages;
            IsPrefabModification = isPrefabModification;
        }

        protected bool Equals(UnityEventHandlerFindResult other)
        {
            return base.Equals(other) && AssetMethodUsages.Equals(other.AssetMethodUsages);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityEventHandlerFindResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ AssetMethodUsages.GetHashCode();
            }
        }
    }
}