#nullable enable
namespace JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches
{
    public abstract class DeclaredElementCacheItemBase
    {
        public string Name { get; }
        public int DeclarationOffset { get; }

        protected DeclaredElementCacheItemBase(string name, int declarationOffset)
        {
            Name = name;
            DeclarationOffset = declarationOffset;
        }
    }
}