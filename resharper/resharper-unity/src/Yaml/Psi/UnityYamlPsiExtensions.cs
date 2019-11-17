using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
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

        public void WriteTo(UnsafeWriter writer)
        { 
            WriteTo(writer, this);
        }

        public static FileID ReadFrom(UnsafeReader reader)
        {
            return new FileID(reader.ReadString(), reader.ReadString());
        }
        
        public static void WriteTo(UnsafeWriter writer, FileID value)
        {
            writer.Write(value.guid);
            writer.Write(value.fileID);
        }
        
        protected bool Equals(FileID other)
        {
            return string.Equals(guid, other.guid) && string.Equals(fileID, other.fileID);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileID) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((guid != null ? guid.GetHashCode() : 0) * 397) ^ (fileID != null ? fileID.GetHashCode() : 0);
            }
        }

        public FileID WithGuid(string newGuid)
        {
            return new FileID(newGuid, fileID);
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

            var searcher = new StringSearcher("&" + anchor, true);
            foreach (var document in file.DocumentsEnumerable)
            {
                // Don't open chameleons unless we have to
                // TODO: GetTextAsBuffer is not cheap - it will allocate a StringBuilder + string
                // But then, FindDocumentByAnchor is hopelessly naive
                if (searcher.Find(document.GetTextAsBuffer()) >= 0)
                {
                    // Note that this opens the Body chameleon
                    var properties = GetDocumentBlockNodeProperties(document.Body.BlockNode);
                    if (properties?.AnchorProperty?.Text?.CompareBufferText(anchor) == true)
                        return document;
                }
            }

            return null;
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static string GetFileId(this IYamlDocument yamlDocument)
        {
            var properties = GetDocumentBlockNodeProperties(yamlDocument.Body.BlockNode);
            return properties?.AnchorProperty?.Text?.GetText();
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

                if (guid == null && fileID == null)
                    return null;
                
                return new FileID(guid, fileID);
            }

            return null;
        }

        // This will open the Body chameleon
        [CanBeNull]
        public static INode GetUnityObjectPropertyValue([CanBeNull] this IYamlDocument document, [NotNull] string key)
        {
            return FindRootBlockMapEntries(document).FindMapEntryBySimpleKey(key)?.Content.Value;
        }

        // This will open the Body chameleon
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
            var rootBlockMappingNode = document?.Body.BlockNode as IBlockMappingNode;
            return rootBlockMappingNode?.EntriesEnumerable.FirstOrDefault()?.Key.AsString();
        }

        // This will open the Body chameleon
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
        public static INodeProperties GetDocumentBlockNodeProperties([CanBeNull] INode documentBlockNode)
        {
            // Careful. This will open chameleons
            if (documentBlockNode is IBlockSequenceNode sequenceNode)
                return sequenceNode.Properties;
            if (documentBlockNode is IBlockMappingNode mappingNode)
                return mappingNode.Properties;
            return null;
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

        public static string GetAnchor(this IYamlDocument document)
        {
            var properties = GetDocumentBlockNodeProperties(document.Body.BlockNode);
            return properties?.AnchorProperty?.Text?.GetText();
        }
        
        public static INode GetValue(this IBlockMappingNode document, string key)
        {
            return document?.Entries.FirstOrDefault(t => t.Key.MatchesPlainScalarText(key))?.Content?.Value;
        }
    }
}