using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetInspectorValues.Values
{
    public interface IAssetValue
    {
        string GetPresentation(ISolution solution, IPersistentIndexManager persistentIndexManager, AssetDocumentHierarchyElementContainer assetDocument, IType type);
    }
}