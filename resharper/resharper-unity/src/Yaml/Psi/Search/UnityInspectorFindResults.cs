using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityInspectorFindResults : UnityAssetFindResult
    {
        public InspectorVariableUsage InspectorVariableUsage { get; }
        public bool IsPrefabModification { get; }

        public UnityInspectorFindResults(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, InspectorVariableUsage inspectorVariableUsage, 
            LocalReference attachedElementLocation, bool isPrefabModification)
            : base(sourceFile, declaredElement, attachedElementLocation)
        {
            InspectorVariableUsage = inspectorVariableUsage;
            IsPrefabModification = isPrefabModification;
        }

        protected bool Equals(UnityInspectorFindResults other)
        {
            return base.Equals(other) && Equals(InspectorVariableUsage, other.InspectorVariableUsage);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityInspectorFindResults) obj);
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