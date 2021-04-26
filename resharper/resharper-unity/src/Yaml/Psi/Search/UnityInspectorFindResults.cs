using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityInspectorFindResult : UnityAssetFindResult
    {
        public InspectorVariableUsage InspectorVariableUsage { get; }
        public bool IsPrefabModification { get; }

        public UnityInspectorFindResult(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, InspectorVariableUsage inspectorVariableUsage, 
            LocalReference owningElemetLocation, bool isPrefabModification)
            : base(sourceFile, declaredElement, owningElemetLocation)
        {
            InspectorVariableUsage = inspectorVariableUsage;
            IsPrefabModification = isPrefabModification;
        }

        protected bool Equals(UnityInspectorFindResult other)
        {
            return base.Equals(other) && Equals(InspectorVariableUsage, other.InspectorVariableUsage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityInspectorFindResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (InspectorVariableUsage != null ? InspectorVariableUsage.GetHashCode() : 0);
            }
        }
    }
}