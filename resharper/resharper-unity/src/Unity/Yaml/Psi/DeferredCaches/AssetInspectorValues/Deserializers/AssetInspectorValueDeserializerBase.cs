using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers
{
    public abstract class AssetInspectorValueDeserializerBase : IAssetInspectorValueDeserializer
    {
        public bool TryGetInspectorValue(IPsiSourceFile owner, IContentNode node, out IAssetValue result)
        {
            result = null;
            if (node is IChameleonNode chameleonNode && !chameleonNode.IsOpened)
            {
                if (!IsInterestingChameleon(chameleonNode))
                    return false;
            }

            result = DeserializeValue(owner, node);
            return result != null;
        }

        [CanBeNull]
        protected abstract IAssetValue DeserializeValue(IPsiSourceFile owner, IContentNode node);
        
        private const int MaxBufferSize = 300;
        protected virtual bool IsInterestingChameleon(IChameleonNode node)
        {
            return node.GetTextAsBuffer().Length < MaxBufferSize;
        }

        public abstract int Order { get; }
    }
}