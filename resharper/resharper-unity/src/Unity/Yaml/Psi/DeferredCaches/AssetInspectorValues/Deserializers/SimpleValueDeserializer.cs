using System;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Deserializers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class SimpleValueDeserializer : AssetInspectorValueDeserializerBase
    {
        private const int MaxSize = 300;
        
        protected override IAssetValue DeserializeValue(IPsiSourceFile owner, IContentNode node)
        {
            if (node is IChameleonNode)
                return null;

            if (node.Value is IPlainScalarNode plainScalarNode)
            {
                var buffer = plainScalarNode.Text.GetTextAsBuffer();
                return new AssetSimpleValue(buffer.GetText(new TextRange(0, Math.Min(buffer.Length, MaxSize))));
            }

            if (node.Value is ISingleQuotedScalarNode singleQuotedScalarNode)
            {
                var buffer = singleQuotedScalarNode.Text.GetTextAsBuffer();
                return new AssetSimpleValue(buffer.GetText(new TextRange(1, Math.Min(buffer.Length - 1, MaxSize))));
            }
            

            return null;
        }

        public override int Order => 0;
    }
}