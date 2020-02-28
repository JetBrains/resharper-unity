using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityScriptsOccurrence : UnityAssetOccurrence
    {
        private readonly string myGuid;

        public UnityScriptsOccurrence(IPsiSourceFile sourceFile,
            IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement, string guid)
            : base(sourceFile, declaredElement, attachedElement)
        {
            myGuid = guid;
        }

        protected bool Equals(UnityScriptsOccurrence other)
        {
            return base.Equals(other) && myGuid == other.myGuid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityScriptsOccurrence) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ myGuid.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"Guid: {myGuid}";
        }
    }
}