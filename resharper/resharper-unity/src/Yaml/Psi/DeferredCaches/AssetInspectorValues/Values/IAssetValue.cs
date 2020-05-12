using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values
{
    public interface IAssetValue
    {
        string GetPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport);
        string GetFullPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport);
    }
}