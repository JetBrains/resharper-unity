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
        public static IHierarchyReference ToHierarchyReference([CanBeNull] this INode node, IPsiSourceFile assetSourceFile)
        {
            if (node is IFlowMappingNode flowMappingNode)
            {
                var localDocumentAnchor = flowMappingNode.GetMapEntryPlainScalarText("fileID");
                if (localDocumentAnchor == null || !long.TryParse(localDocumentAnchor, out var result))
                    return new LocalReference(0, 0);

                if (result == 0)
                    return LocalReference.Null;

                var externalAssetGuid = flowMappingNode.GetMapEntryPlainScalarText("guid");

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
        public static IYamlDocument GetFirstMatchingUnityObjectDocument([CanBeNull] this IYamlFile file, [NotNull] string objectType)
        {
            if (file == null)
                return null;

            foreach (var document in file.DocumentsEnumerable)
            {
                if (document.Body.BlockNode is IBlockMappingNode map)
                {
                    // Object type will be the first entry. If it's the required document, return it. For simple assets,
                    // such as scriptable objects, there will be only one document, most likely of the expected type.
                    // For other assets, such as scenes, there can be many documents, and can be many matching object
                    // documents. This will get the first
                    if (map.GetMapEntry(objectType) != null)
                        return document;
                }
            }

            return null;
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static T GetUnityObjectPropertyValue<T>([CanBeNull] this IYamlFile file, [NotNull] string objectType,
                                                       [NotNull] string key)
            where T : class, INode
        {
            return file.GetFirstMatchingUnityObjectDocument(objectType)?.GetUnityObjectPropertyValue<T>(key);
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static T GetUnityObjectPropertyValue<T>([CanBeNull] this IYamlDocument document, [NotNull] string key)
            where T : class, INode
        {
            // Get the object's properties as a map, and find the property by name
            return GetUnityObjectProperties(document).GetMapEntryValue<T>(key);
        }

        // TODO: Consider adding GetUnityObjectPropertyArray
        // This would return IBlockSequenceNode for a populated array, or IFlowSequenceNode (with no children) for an
        // empty array. This would require adding a base ISequenceNode interface which would also help (although block
        // sequence nodes have a Content chameleon and flow sequence nodes don't)

        // This will open the Body chameleon
        [CanBeNull]
        public static IBlockMappingNode GetUnityObjectProperties([CanBeNull] this IYamlDocument document)
        {
            // A YAML document has a single root body node, which can more or less be anything (scalar, map or sequence
            // - it's in a block context, but that only affects parsing, and not much). For a Unity YAML document, the
            // root node is a block map, with the key being the type of the serialised object (e.g. MonoBehaviour,
            // GameObject, Transform) and the value being another block mapping mode. This method returns the second
            // block map, which represent the properties of the object. E.g. the m_* values here:
            // GameObject:
            //   m_ObjectHideFlags: 0
            //   m_CorrespondingSourceObject: ...
            var rootBlockMappingNode = document?.Body.BlockNode as IBlockMappingNode;
            return rootBlockMappingNode?.EntriesEnumerable.FirstOrDefault()?.Content.Value as IBlockMappingNode;
        }
    }
}