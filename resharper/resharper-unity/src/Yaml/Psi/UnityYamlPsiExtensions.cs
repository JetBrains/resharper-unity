using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    // ReSharper disable InconsistentNaming
    public class FileID
    {
        public static readonly FileID Null = new FileID(null, "0");

        public readonly string guid;
        public readonly string fileID;
        // public string type;    // I don't know what this type means

        // Equivalent to a null pointer. We have null and IsNullReference, because null indicates missing (we've
        // probably parsed something wrong), while  IsNullReference indicates that it's explicitly set to null
        public bool IsNullReference => this == Null || fileID == "0";

        // Is external to the current file. True if there's an asset GUID
        public bool IsExternal => guid != null;

        // The static value that represents a MonoScript asset (C# scripts can't have an ID inside the file)
        public bool IsMonoScript => fileID == "11500000";

        public FileID(string guid, string fileID)
        {
            this.guid = guid;
            this.fileID = fileID;
        }

        public override string ToString()
        {
            return $"FileID: {fileID}, {guid ?? "<no guid>"}";
        }
    }
    // ReSharper restore InconsistentNaming

    public static class UnityYamlPsiExtensions
    {
        [CanBeNull]
        public static IYamlDocument FindDocumentByAnchor([CanBeNull] this IYamlFile file, [CanBeNull] string anchor)
        {
            if (file == null || anchor == null)
                return null;

            foreach (var document in file.DocumentsEnumerable)
            {
                var properties = GetDocumentBlockNodeProperties(document.BlockNode);
                var text = properties?.AnchorProperty?.Text?.GetText() ?? string.Empty;
                if (text == anchor)
                    return document;
            }

            return null;
        }

        [CanBeNull]
        public static string AsString([CanBeNull] this INode node)
        {
            return node?.GetPlainScalarText();
        }

        [CanBeNull]
        public static FileID AsFileID([CanBeNull] this INode node)
        {
            if (node is IFlowMappingNode flowMappingNode)
            {
                var fileID = flowMappingNode.FindMapEntryBySimpleKey("fileID")?.Value.AsString();
                if (fileID == "0")
                    return FileID.Null;
                var guid = flowMappingNode.FindMapEntryBySimpleKey("guid")?.Value.AsString();
                return new FileID(guid, fileID);
            }

            return null;
        }

        [CanBeNull]
        public static INode GetUnityObjectPropertyValue([CanBeNull] this IYamlDocument document, [NotNull] string key)
        {
            return FindRootBlockMapEntries(document).FindMapEntryBySimpleKey(key)?.Value;
        }

        [CanBeNull]
        public static string GetUnityObjectTypeFromRootNode([CanBeNull] this IYamlDocument document)
        {
            // E.g.
            // --- !u!114 &293532596
            // MonoBehaviour:
            //   m_ObjectHideFlags: 0
            // This will return "MonoBehaviour"
            // (Note that !u!114 is the actual type of this object - MonoBehaviour -
            // https://docs.unity3d.com/Manual/ClassIDReference.html)
            var rootBlockMappingNode = document?.BlockNode as IBlockMappingNode;
            return rootBlockMappingNode?.EntriesEnumerable.FirstOrDefault()?.Key.AsString();
        }

        [CanBeNull]
        public static IYamlDocument GetUnityObjectDocumentFromFileIDProperty([CanBeNull] this IYamlDocument document, string key)
        {
            var fileID = document.GetUnityObjectPropertyValue(key).AsFileID();
            if (fileID == null || fileID.IsNullReference || fileID.IsExternal)
                return null;

            Assertion.AssertNotNull(document, "document != null");
            var file = (IYamlFile) document.GetContainingFile();
            return file.FindDocumentByAnchor(fileID.fileID);
        }

        [CanBeNull]
        private static INodeProperties GetDocumentBlockNodeProperties([CanBeNull] INode documentBlockNode)
        {
            if (documentBlockNode is IBlockSequenceNode sequenceNode)
                return sequenceNode.Properties;
            if (documentBlockNode is IBlockMappingNode mappingNode)
                return mappingNode.Properties;
            return null;
        }

        [CanBeNull]
        private static IBlockMappingNode FindRootBlockMapEntries([CanBeNull] this IYamlDocument document)
        {
            // A YAML document is a block mapping node with a single entry. The key is usually the type of the object,
            // while the value is another block mapping node. Those entries are the properties of the Unity object
            var rootBlockMappingNode = document?.BlockNode as IBlockMappingNode;
            return rootBlockMappingNode?.EntriesEnumerable.FirstOrDefault()?.Value as IBlockMappingNode;
        }
    }
}