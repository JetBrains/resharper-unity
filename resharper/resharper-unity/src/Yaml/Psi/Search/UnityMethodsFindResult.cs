using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityMethodsFindResult : UnityAssetFindResult
    {
        public UnityMethodsFindResult(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, TextRange textRange, IHierarchyElement parent)
            : base(sourceFile, declaredElement, textRange, parent)
        {
        }
    }
}