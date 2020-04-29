using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityEventFindResult : FindResultDeclaredElement
    {
        public UnityEventFindResult([NotNull] IDeclaredElement declaredElement, IPsiSourceFile sourceFile, LocalReference attachedElementLocation, bool isPrefabModification) : base(declaredElement)
        {
            SourceFile = sourceFile;
            AttachedElementLocation = attachedElementLocation;
            IsPrefabModification = isPrefabModification;
        }

        public IPsiSourceFile SourceFile { get; }
        public LocalReference AttachedElementLocation { get; }
        public bool IsPrefabModification { get; }


        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}