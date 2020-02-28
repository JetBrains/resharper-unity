using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityMethodsOccurrence : UnityAssetOccurrence
    {
        private readonly AssetMethodData myMethodData;

        public UnityMethodsOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement, AssetMethodData methodData)
            : base(sourceFile, declaredElement, attachedElement)
        {
            myMethodData = methodData;
        }

        protected bool Equals(UnityMethodsOccurrence other)
        {
            return base.Equals(other) && myMethodData.Equals(other.myMethodData);
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
                return (base.GetHashCode() * 397) ^ myMethodData.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"m_MethodName: {myMethodData.MethodName}";
        }
    }
}