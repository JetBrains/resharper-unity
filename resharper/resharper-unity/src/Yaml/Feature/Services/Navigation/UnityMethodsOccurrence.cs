using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    public class UnityMethodsOccurrence : UnityAssetOccurrence
    {
        public UnityMethodsOccurrence(IPsiSourceFile sourceFile, IDeclaredElementPointer<IDeclaredElement> declaredElement, IHierarchyElement attachedElement)
            : base(sourceFile, declaredElement, attachedElement)
        {
        }
    }
}