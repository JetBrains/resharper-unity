using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    internal class TypeDocument
    {
        // "Namespace:" is only used in 5.0
        private static readonly Regex CaptureKindAndNamespaceRegex = new Regex(@"^(((?<type>class|struct|interface) in|Namespace:)\W*(?<namespace>\w+(?:\.\w+)*)|(?<type>enumeration))$", RegexOptions.Compiled);

        [CanBeNull]
        internal static TypeDocument Load(string fileName, string fullName)
        {
            var content = File.ReadAllText(fileName);
            if (!content.Contains("class in") && !content.Contains("struct in") && !content.Contains("interface in") && !content.Contains("enumeration") && !content.Contains("Namespace:"))
                return null;

            try
            {
                var document = new TypeDocument(content, fileName, fullName);
                if (string.IsNullOrEmpty(document.ShortName) || string.IsNullOrEmpty(document.Kind))
                    return null;
                return document;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading file: {fileName}: " + e);
                return null;
            }
        }

        private TypeDocument(string content, string docPath, string fullName)
        {
            DocPath = docPath;
            var documentNode = SimpleHtmlNode.LoadContent(content);

            ShortName = documentNode.SelectOne(@"//div.content/div.section/div.mb20.clear/h1")?.Text
                        ?? throw new InvalidDataException("Cannot find short name");

            // So we can return early
            Messages = EmptyArray<SimpleHtmlNode>.Instance;
            Kind = Namespace = string.Empty;

            // "class in {ns}"/"struct in {ns}"/"Namespace: {ns}"
            var namespaceParagraph = documentNode.SelectOne(@"//div.content/div.section/div.mb20.clear/p");
            if (namespaceParagraph == null)
                return;

            var match = CaptureKindAndNamespaceRegex.Match(namespaceParagraph.Text);
            Kind = match.Groups["type"].Value;
            Namespace = match.Groups["namespace"].Value;

            if (string.IsNullOrEmpty(Kind)) Kind = "class";
            if (string.IsNullOrEmpty(Namespace))
            {
                var index = fullName.LastIndexOf('.');
                if (index == -1)
                    throw new InvalidDataException($"Cannot get namespace from full type name: {fullName}");
                Namespace = fullName.Substring(0, index);
            }

            Messages = documentNode.SelectMany(
                @"//div.content/div.section/div.subsection[h2='Messages']/table.list//tr");

            var removedDiv =
                documentNode.SelectOne(@"//div.content/div.section/div.mb20.clear/div[@class='message message-error mb20']");
            IsRemoved = removedDiv != null && removedDiv.Text.StartsWith("Removed");
        }

        [NotNull] public string DocPath { get; private set; }
        [NotNull] public string ShortName { get; private set; }
        // Namespace is not guaranteed to be 100% correct. Enums are missing the UnityEngine. or UnityEditor. prefix
        [NotNull] public string Namespace { get; private set; }
        public string FullName => string.IsNullOrEmpty(Namespace) ? ShortName : Namespace + "." + ShortName;
        [NotNull] public string Kind { get; private set; }
        public bool IsRemoved { get; private set; }
        [NotNull] public SimpleHtmlNode[] Messages { get; private set; }
    }
}