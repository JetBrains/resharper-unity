using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers
{
    public interface IAssetInspectorValueDeserializer
    {
        bool TryGetInspectorValue(IPsiSourceFile owner, IContentNode node, out IAssetValue result);
        
        int Order { get; }
    }
}