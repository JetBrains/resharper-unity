#nullable enable
using System;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    public static class UnityYamlPsiExtensions
    {
        public static string? GetUnicodeText(this INode node)
        {
            return node switch
            {
                IDoubleQuotedScalarNode doubleQuotedScalarNode => DecodeText(doubleQuotedScalarNode.Text.GetText()),
                _ => node.GetScalarText() 
            };
        }

        private static string? DecodeText(string text)
        {
            if (text.Length < 2 || text[0] != '"' || text[^1] != '"')
                return null;
            if (text.Length == 2)
                return string.Empty;
            var result = new StringBuilder(text.Length - 2);
            ProcessEscapedString(text.AsSpan(1, text.Length - 2), result);
            return result.ToString();
        }
        
        private static void ProcessEscapedString(ReadOnlySpan<char> str, StringBuilder output)
        {
            const char escapeCharacter = '\\';
            const char quoteCharacter = '"';
            
            var inEscapeSequence = false;
            while (true)
            {
                str_loop: 
                var index = 0;
                foreach (var ch in str)
                {
                    ++index;
                    if (inEscapeSequence)
                    {
                        inEscapeSequence = false;
                        
                        switch (ch)
                        {
                            case 'x' when TryDecodeHexSequence(ref str, index) is { } decodedChar:
                                output.Append(decodedChar);
                                goto str_loop;
                            case 'u' when TryDecodeUnicodeSequence(ref str, index) is { } decodedChar:
                                output.Append(decodedChar);
                                goto str_loop;
                            case escapeCharacter:
                            case quoteCharacter:
                                output.Append(ch);
                                break;
                            default:
                                output.Append(escapeCharacter);
                                output.Append(ch);
                                break;
                        }
                    }
                    else
                    {
                        if (ch != escapeCharacter)
                            output.Append(ch);
                        else
                            inEscapeSequence = true;
                    }
                }
                break;
            }
        }

        private static char? TryDecodeHexSequence(ref ReadOnlySpan<char> str, int index)
        {
            try
            {
                if (str.Length - index < 2)
                    return null;

                var ch = (char)((Uri.FromHex(str[index]) << 4) + Uri.FromHex(str[index + 1]));
                str = str.Slice(index + 2);
                return ch;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
        
        private static char? TryDecodeUnicodeSequence(ref ReadOnlySpan<char> str, int index)
        {
            try
            {
                if (str.Length - index < 4)
                    return null;
                    
                var ch = (char)((Uri.FromHex(str[index]) << 12) + (Uri.FromHex(str[index + 1]) << 8) + (Uri.FromHex(str[index + 2]) << 4) + Uri.FromHex(str[index + 3]));
                str = str.Slice(index + 4);
                return ch;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static IHierarchyReference? ToHierarchyReference(this INode? node, IPsiSourceFile assetSourceFile)
        {
            if (node is IFlowMappingNode flowMappingNode)
            {
                var localDocumentAnchor = flowMappingNode.GetMapEntryScalarText("fileID");
                if (localDocumentAnchor == null || !long.TryParse(localDocumentAnchor, out var result))
                    return new LocalReference(0, 0);

                if (result == 0)
                    return LocalReference.Null;

                var externalAssetGuid = flowMappingNode.GetMapEntryScalarText("guid");
                if (externalAssetGuid == null)
                    return new LocalReference(assetSourceFile.PsiStorage.PersistentIndex.NotNull("owningPsiPersistentIndex != null"), result);

                if (Guid.TryParse(externalAssetGuid, out var guid))
                    return new ExternalReference(guid, result);

                return LocalReference.Null;
            }

            return null;
        }

        // This will open the Body chameleon
        public static IYamlDocument? GetFirstMatchingUnityObjectDocument(this IYamlFile? file, string objectType)
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
        public static T? GetUnityObjectPropertyValue<T>(this IYamlFile? file, string objectType, string key)
            where T : class, INode
        {
            return file.GetFirstMatchingUnityObjectDocument(objectType)?.GetUnityObjectPropertyValue<T>(key);
        }

        // This will open the Body chameleon
        public static T? GetUnityObjectPropertyValue<T>(this IYamlDocument? document, string key)
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
        public static IBlockMappingNode? GetUnityObjectProperties(this IYamlDocument? document)
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