using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityScriptsFindResults : UnityAssetFindResult
    {
        public AssetScriptUsages AssetScriptUsages { get; }

        public UnityScriptsFindResults(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, AssetScriptUsages assetScriptUsages, 
            LocalReference attachedElementLocation)
            : base(sourceFile, declaredElement, attachedElementLocation)
        {
            AssetScriptUsages = assetScriptUsages;
        }

        protected bool Equals(UnityScriptsFindResults other)
        {
            return base.Equals(other) && AssetScriptUsages.Equals(other.AssetScriptUsages);
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
                return (base.GetHashCode() * 397) ^ AssetScriptUsages.GetHashCode();
            }
        }
    }
}