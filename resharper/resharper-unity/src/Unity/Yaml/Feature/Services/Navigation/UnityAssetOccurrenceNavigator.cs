using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityAssetOccurrenceNavigator
    {
        public virtual bool Navigate(ISolution solution, IDeclaredElementPointer<IDeclaredElement> pointer, LocalReference location)
        {
            return true;
        }
    }
}