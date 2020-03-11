using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers
{
    [SolutionComponent]
    public class ReferenceValueDeserializer : AssetInspectorValueDeserializerBase
    {
        protected override IAssetValue DeserializeValue(IPsiSourceFile owner, IContentNode node)
        {
            if (AssetUtils.IsReferenceValue(node.GetTextAsBuffer()))
            {
                var fileId = node.Value.AsFileID();
                if (fileId == null)
                    return null;
                
                return new AssetReferenceValue(fileId.ToReference(owner));
            }

            return null;
        }

        public override int Order => 1;
    }
}