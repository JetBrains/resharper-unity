using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityScriptsFindResults : UnityAssetFindResult
    {
        public UnityScriptsFindResults(IPsiSourceFile sourceFile, IDeclaredElement declaredElement, TextRange textRange, IHierarchyElement parent)
            : base(sourceFile, declaredElement, textRange, parent)
        {
        }
    }
}