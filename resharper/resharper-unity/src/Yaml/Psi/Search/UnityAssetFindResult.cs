using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public abstract class UnityAssetFindResult : FindResult
    {
        public IPsiSourceFile SourceFile { get; }
        public IDeclaredElementPointer<IDeclaredElement> DeclaredElementPointer { get; }
        public IHierarchyElement AttachedElement { get; }
        public LocalReference AttachedElementLocation { get; }

        protected UnityAssetFindResult(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, IHierarchyElement attachedElement, LocalReference attachedElementLocation)
        {
            SourceFile = sourceFile;
            AttachedElement = attachedElement;
            AttachedElementLocation = attachedElementLocation;
            DeclaredElementPointer = new SourceElementPointer<IDeclaredElement>(declaredElement);
        }
        

        protected bool Equals(UnityAssetFindResult other)
        {
            return SourceFile.Equals(other.SourceFile) && AttachedElement.Equals(other.AttachedElement);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnityAssetFindResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SourceFile.GetHashCode();
                hashCode = (hashCode * 397) ^ AttachedElement.GetHashCode();
                return hashCode;
            }
        }
    }
}