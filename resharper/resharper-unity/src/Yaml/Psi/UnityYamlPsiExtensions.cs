using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityYamlPsiExtensions
    {
        [CanBeNull]
        public static string AsString([CanBeNull] this INode node)
        {
            return node?.GetPlainScalarText();
        }

        [CanBeNull]
        public static IHierarchyReference ToHierarchyReference([CanBeNull] this INode node, IPsiSourceFile assetSourceFile)
        {
            if (node is IFlowMappingNode flowMappingNode)
            {
                var localDocumentAnchor = flowMappingNode.FindMapEntryBySimpleKey("fileID")?.Value.AsString();
                if (localDocumentAnchor == null || !long.TryParse(localDocumentAnchor, out var result))
                    return new LocalReference(0, 0);

                if (result == 0)
                    return LocalReference.Null;

                var externalAssetGuid = flowMappingNode.FindMapEntryBySimpleKey("guid")?.Value.AsString();

                if (externalAssetGuid == null)
                {
                    if (result == 0)
                        return new LocalReference(0, 0);

                    return new LocalReference(assetSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), result);
                }

                if (Guid.TryParse(externalAssetGuid, out var guid))
                    return new ExternalReference(guid, result);

                return LocalReference.Null;
            }

            return null;
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static INode GetUnityObjectPropertyValue([CanBeNull] this IYamlDocument document, [NotNull] string key)
        {
            // Get the object's properties as a map, and find the property by name
            return FindRootBlockMapEntries(document).FindMapEntryBySimpleKey(key)?.Content.Value;
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static IBlockMappingNode FindRootBlockMapEntries([CanBeNull] this IYamlDocument document)
        {
            // A YAML document has a single root body node, which can more or less be anything (scalar, map or sequence
            // - it's in a block context, but that only affects parsing, and not much). For a Unity YAML document, the
            // root node is a block map, with the key being the type of the serialised object (e.g. MonoBehaviour,
            // GameObject, Transform) and the value being another block mapping mode. The entries of this map are the
            // properties of the Unity object.
            var rootBlockMappingNode = document?.Body.BlockNode as IBlockMappingNode;
            return rootBlockMappingNode?.EntriesEnumerable.FirstOrDefault()?.Content.Value as IBlockMappingNode;
        }

        public static INode GetValue(this IBlockMappingNode document, string key)
        {
            return document.FindMapEntryBySimpleKey(key)?.Content?.Value;
        }
    }
}