using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class AnimExplicitFindResults : UnityAssetFindResult
    {
        public AnimExplicitFindResults([NotNull] IPsiSourceFile sourceFile,
                                              [NotNull] IDeclaredElement declaredElement,
                                              [NotNull] AnimExplicitUsage usage,
                                              LocalReference owningElementLocation)
            : base(sourceFile, declaredElement, owningElementLocation)
        {
            Usage = usage;
        }

        [NotNull]
        public AnimExplicitUsage Usage { get; }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((AnimExplicitFindResults) obj);
        }

        private bool Equals([NotNull] AnimExplicitFindResults other)
        {
            return base.Equals(other) && Usage.Equals(other.Usage);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Usage.GetHashCode();
            }
        }
    }
}