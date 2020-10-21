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
        public static IHierarchyReference ToHierarchyReference([CanBeNull] this INode node, [NotNull] IPsiSourceFile assetSourceFile)
        {
            var (anchor, guid) = node.ExtractAnchorAndGuid();
            if (anchor is null || anchor == 0) return LocalReference.Null;
            if (guid != null) return new ExternalReference(guid.Value, anchor.Value);
            var persistentIndex = assetSourceFile
                .PsiStorage
                .PersistentIndex
                .NotNull("owningPsiPersistentIndex != null");
            return new LocalReference(persistentIndex, anchor.Value);
        }
        
        public static (long?, Guid?) ExtractAnchorAndGuid([CanBeNull] this INode node)
        {
            if (!(node is IFlowMappingNode flowMappingNode)) return (null, null);
            var anchor = flowMappingNode.FindMapEntryBySimpleKey("fileID")?.Value.AsString();
            if (anchor == null || !long.TryParse(anchor, out var result)) return (null, null);
            if (result == 0) return (0, null);
            var scriptGuid = flowMappingNode.FindMapEntryBySimpleKey("guid")?.Value.AsString();
            if (scriptGuid == null || !Guid.TryParse(scriptGuid, out var guid)) return (result, null);
            return (result, guid);

        }

        // This will open the Body chameleon
        [CanBeNull]
        public static INode GetUnityObjectPropertyValue([CanBeNull] this IYamlDocument document, [NotNull] string key)
        {
            return FindRootBlockMapEntries(document).FindMapEntryBySimpleKey(key)?.Content.Value;
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static IBlockMappingNode FindRootBlockMapEntries([CanBeNull] this IYamlDocument document)
        {
            // A YAML document is a block mapping node with a single entry. The key is usually the type of the object,
            // while the value is another block mapping node. Those entries are the properties of the Unity object
            var rootBlockMappingNode = document?.Body.BlockNode as IBlockMappingNode;
            return rootBlockMappingNode?.EntriesEnumerable.FirstOrDefault()?.Content.Value as IBlockMappingNode;
        }

        public static INode GetValue(this IBlockMappingNode document, string key)
        {
            return document?.Entries.FirstOrDefault(t => t.Key.MatchesPlainScalarText(key))?.Content?.Value;
        }
    }
}