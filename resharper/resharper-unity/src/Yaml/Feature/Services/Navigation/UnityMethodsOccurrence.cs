using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityMethodsOccurrence : UnityAssetOccurrence
    {
        public readonly AssetMethodData MethodData;

        public UnityMethodsOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement, AssetMethodData methodData)
            : base(sourceFile, declaredElement, attachedElement)
        {
            MethodData = methodData;
        }

        protected bool Equals(UnityMethodsOccurrence other)
        {
            return base.Equals(other) && MethodData.Equals(other.MethodData);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityMethodsOccurrence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ MethodData.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"m_MethodName: {MethodData.MethodName}";
        }
    }
}