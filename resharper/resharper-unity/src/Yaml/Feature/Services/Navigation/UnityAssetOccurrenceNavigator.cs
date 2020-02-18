using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    [SolutionComponent]
    public class UnityAssetOccurrenceNavigator
    {
        public virtual bool Navigate(ISolution solution, IDeclaredElementPointer<IDeclaredElement> pointer,IHierarchyElement parent)
        {
            return true;
        }
    }
}