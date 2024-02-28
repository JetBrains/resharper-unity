using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class ReferenceValueDeserializer : AssetInspectorValueDeserializerBase
    {
        protected override IAssetValue DeserializeValue(IPsiSourceFile owner, IContentNode node)
        {
            if (AssetUtils.IsReferenceValue(node.GetTextAsBuffer()))
            {
                var reference = node.Value.ToHierarchyReference(owner);
                if (reference == null)
                    return null;

                return new AssetReferenceValue(reference);
            }

            return null;
        }

        public override int Order => 1;
    }
}