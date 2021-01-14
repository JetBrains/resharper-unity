using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityAnimationEventFindResults : UnityAssetFindResult
    {
        public UnityAnimationEventFindResults([NotNull] IPsiSourceFile sourceFile,
                                              [NotNull] IDeclaredElement declaredElement,
                                              [NotNull] AnimationUsage usage,
                                              LocalReference owningElementLocation)
            : base(sourceFile, declaredElement, owningElementLocation)
        {
            Usage = usage;
        }

        [NotNull]
        public AnimationUsage Usage { get; }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((UnityAnimationEventFindResults) obj);
        }

        private bool Equals([NotNull] UnityAnimationEventFindResults other)
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