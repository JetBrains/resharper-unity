using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace ApiParser
{
    internal class TypeDocument
    {
        // "Namespace:" is only used in 5.0
        private static readonly Regex CaptureKindAndNamespaceRegex = new Regex(@"^(((?<type>class|struct|interface) in|Namespace:)\W*(?<namespace>\w+(?:\.\w+)*)|(?<type>enumeration))$", RegexOptions.Compiled);

        [CanBeNull]
        internal static TypeDocument Load(string fileName, string fullName, string langCode)
        {
            try
            {
                var content = File.ReadAllText(fileName);
                if (!content.Contains("class in") && !content.Contains("struct in") && !content.Contains("interface in") && !content.Contains("enumeration") && !content.Contains("Namespace:"))
                    return null;

                var documentNode = SimpleHtmlNode.LoadContent(content);
                var shortName = documentNode.SelectOne(@"//div.content/div.section/div.mb20.clear/h1")?.Text
                        ?? throw new InvalidDataException("Cannot find short name");

                // "class in {ns}"/"struct in {ns}"/"Namespace: {ns}"
                var namespaceParagraph = documentNode.SelectOne(@"//div.content/div.section/div.mb20.clear/p");
                if (namespaceParagraph != null)
                {
                    var match = CaptureKindAndNamespaceRegex.Match(namespaceParagraph.Text);
                    var kind = match.Groups["type"].Value;
                    var ns = match.Groups["namespace"].Value;

                    if (string.IsNullOrEmpty(kind)) kind = "class";
                    if (string.IsNullOrEmpty(ns))
                    {
                        var index = fullName.LastIndexOf('.');
                        if (index == -1)
                            throw new InvalidDataException($"Cannot get namespace from full type name: {fullName}");
                        ns = fullName.Substring(0, index);
                    }

                    var messagesText = LocalizationUtil.GetMessagesDivTextByLangCode(langCode);
                    var messages = documentNode.SelectMany(
                        $@"//div.content/div.section/div.subsection[h2='{messagesText}' or h3='{messagesText}']/table.list//tr");

                    var removedDiv =
                        documentNode.SelectOne(
                            @"//div.content/div.section/div.mb20.clear/div[@class='message message-error mb20']");
                    var isRemoved = removedDiv != null && removedDiv.Text.StartsWith("Removed");

                    return new TypeDocument(fileName, shortName, ns, kind, isRemoved, messages);
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading file: {fileName}: " + e);
                return null;
            }
        }

        private TypeDocument([NotNull] string docPath, [NotNull] string shortName, [NotNull] string ns,
            [NotNull] string kind, bool isRemoved, [NotNull] SimpleHtmlNode[] messages)
        {
            DocPath = docPath;
            ShortName = shortName;
            Namespace = ns;
            Kind = kind;
            IsRemoved = isRemoved;
            Messages = messages;
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