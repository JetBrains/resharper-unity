using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    // ReSharper disable InconsistentNaming
    public class AssetDocumentReference
    {
        public static readonly AssetDocumentReference Null = new AssetDocumentReference(null, "0");

        public readonly string ExternalAssetGuid; 
        public readonly string LocalDocumentAnchor;
        
        // public string type;    // I don't know what this type means

        // Equivalent to a null pointer. We have null and IsNullReference, because null indicates missing (we've
        // probably parsed something wrong), while  IsNullReference indicates that it's explicitly set to null
        public bool IsNullReference => this == Null || LocalDocumentAnchor == "0";

        // Is external to the current file. True if there's an asset GUID
        public bool IsExternal => ExternalAssetGuid != null;

        // The static value that represents a MonoScript asset (C# scripts can't have an ID inside the file)
        public bool IsMonoScript => LocalDocumentAnchor == "11500000";

        public AssetDocumentReference(string externalAssetGuid, string localDocumentAnchor)
        {
            this.ExternalAssetGuid = externalAssetGuid;
            this.LocalDocumentAnchor = localDocumentAnchor;
        }

        public ulong? AnchorLong => ulong.TryParse(LocalDocumentAnchor, out var result) ? result : (ulong?)null;

        public override string ToString()
        {
            return $"FileID: {LocalDocumentAnchor}, {ExternalAssetGuid ?? "<no guid>"}";
        }

        public void WriteTo(UnsafeWriter writer)
        { 
            WriteTo(writer, this);
        }

        public static AssetDocumentReference ReadFrom(UnsafeReader reader)
        {
            return new AssetDocumentReference(reader.ReadString(), reader.ReadString());
        }
        
        
        public static void WriteTo(UnsafeWriter writer, AssetDocumentReference value)
        {
            writer.Write(value.ExternalAssetGuid);
            writer.Write(value.LocalDocumentAnchor);
        }
        
        protected bool Equals(AssetDocumentReference other)
        {
            return string.Equals(ExternalAssetGuid, other.ExternalAssetGuid) && string.Equals(LocalDocumentAnchor, other.LocalDocumentAnchor);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetDocumentReference) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ExternalAssetGuid != null ? ExternalAssetGuid.GetHashCode() : 0) * 397) ^ (LocalDocumentAnchor != null ? LocalDocumentAnchor.GetHashCode() : 0);
            }
        }

        public AssetDocumentReference WithGuid(string newGuid)
        {
            return new AssetDocumentReference(newGuid, LocalDocumentAnchor);
        }

        public IHierarchyReference ToReference(IPsiSourceFile currentSourceFile)
        {
            if (!ulong.TryParse(LocalDocumentAnchor, out var result))
                return null;

            if (ExternalAssetGuid == null)
            {
                if (result == 0)
                    return null;
                
                return new LocalReference(currentSourceFile.PsiStorage.PersistentIndex, result);
            }

            return new ExternalReference(ExternalAssetGuid, result);
        }
    }
    // ReSharper restore InconsistentNaming

    public static class UnityYamlPsiExtensions
    {
        [CanBeNull]
        public static string AsString([CanBeNull] this INode node)
        {
            return node?.GetPlainScalarText();
        }

        [CanBeNull]
        public static AssetDocumentReference AsFileID([CanBeNull] this INode node)
        {
            if (node is IFlowMappingNode flowMappingNode)
            {
                var fileID = flowMappingNode.FindMapEntryBySimpleKey("fileID")?.Value.AsString();
                if (fileID == "0")
                    return AssetDocumentReference.Null;
                var guid = flowMappingNode.FindMapEntryBySimpleKey("guid")?.Value.AsString();

                if (guid == null && fileID == null)
                    return null;
                
                return new AssetDocumentReference(guid, fileID);
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