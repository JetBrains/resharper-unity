using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
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
        public TextRange TextRange { get; }
        public IHierarchyElement Parent { get; }

        protected UnityAssetFindResult(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, TextRange textRange, IHierarchyElement parent)
        {
            SourceFile = sourceFile;
            TextRange = textRange;
            Parent = parent;
            DeclaredElementPointer = new SourceElementPointer<IDeclaredElement>(declaredElement);
        }
        

        protected bool Equals(UnityAssetFindResult other)
        {
            return SourceFile.Equals(other.SourceFile) && TextRange.Equals(other.TextRange) && Parent.Equals(other.Parent);
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
                hashCode = (hashCode * 397) ^ TextRange.GetHashCode();
                hashCode = (hashCode * 397) ^ Parent.GetHashCode();
                return hashCode;
            }
        }
    }
}