using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class AnimImplicitFindResult : FindResult
    {
        [NotNull] public IDeclaredElement DeclaredElement { get; }
        [NotNull] public readonly AnimImplicitUsage Usage;

        public AnimImplicitFindResult([NotNull] IPsiSourceFile sourceFile,
            [NotNull] IDeclaredElement declaredElement,
            [NotNull] AnimImplicitUsage animImplicitUsage)
        {
            DeclaredElement = declaredElement;
            Usage = animImplicitUsage;
        }

        public override bool Equals(object obj)
        {
            return obj is AnimImplicitFindResult findResult && findResult.Usage.Equals(Usage);
        }

        public override int GetHashCode()
        {
            return Usage.GetHashCode();
        }
    }
}