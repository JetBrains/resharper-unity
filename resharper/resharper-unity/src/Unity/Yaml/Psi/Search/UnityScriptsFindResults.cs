using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityScriptsFindResults : UnityAssetFindResult
    {
        public IScriptUsage ScriptUsage { get; }

        public UnityScriptsFindResults(IPsiSourceFile sourceFile,
                                       IDeclaredElement declaredElement,
                                       IScriptUsage scriptUsage, 
                                       LocalReference owningElementLocation)
            : base(sourceFile, declaredElement, owningElementLocation)
        {
            ScriptUsage = scriptUsage;
        }

        protected bool Equals(UnityScriptsFindResults other)
        {
            return base.Equals(other) && ScriptUsage.Equals(other.ScriptUsage);
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
                return (base.GetHashCode() * 397) ^ ScriptUsage.GetHashCode();
            }
        }
    }
}