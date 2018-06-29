using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public sealed class ApiNode
    {
        private readonly HtmlNode node;

        private ApiNode([NotNull] HtmlNode node)
        {
            this.node = node;
        }

        [NotNull]
        private string Code => node.InnerHtml;

        [NotNull]
        public string Text
        {
            get
            {
                // Fix up dodgy HTML in example text for Unity 5.5
                if (node.Name == "pre")
                {
                    var text = "";
                    foreach (var childNode in node.ChildNodes)
                    {
                        if (childNode.NodeType == HtmlNodeType.Element && childNode.Name == "br")
                            text += Environment.NewLine;
                        else
                            text += childNode.InnerText;
                    }
                    return text;
                }
                return node.InnerText.Trim();
            }
        }

        [CanBeNull]
        public ApiNode this[int index] => Wrap(node.ChildNodes[index]);

        [NotNull]
        public string this[[NotNull] string attributeName] => node.GetAttributeValue(attributeName, string.Empty);

        [CanBeNull]
        public static ApiNode Load([NotNull] string path)
        {
            if (!File.Exists(path)) return null;
            var document = new HtmlDocument();
            document.Load(path);
            return new ApiNode(document.DocumentNode);
        }

        [CanBeNull]
        public static ApiNode LoadContent(string content)
        {
            var document = new HtmlDocument();
            document.LoadHtml(content);
            return new ApiNode(document.DocumentNode);
        }

        [NotNull]
        public ApiNode[] SelectMany([NotNull] string xpath)
        {
            var nodes = node.SelectNodes(XPath.Resolve(xpath));
            return nodes?.Select(Wrap).ToArray() ?? EmptyArray<ApiNode>.Instance;
        }

        [CanBeNull]
        public ApiNode SelectOne([NotNull] string xpath)
        {
            return Wrap(node.SelectSingleNode(XPath.Resolve(xpath)));
        }

        [NotNull]
        public IEnumerable<ApiNode> Subsection([NotNull] string name)
        {
            return SelectMany($@"div.subsection[h2='{name}']/table.list//tr");
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return Code;
        }

        [CanBeNull]
        private static ApiNode Wrap([CanBeNull] HtmlNode node)
        {
            return node != null ? new ApiNode(node) : null;
        }
    }
}
