using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using JetBrains.Annotations;
using JetBrains.Util;

namespace ApiParser
{
    public sealed class SimpleHtmlNode
    {
        private readonly HtmlNode myNode;

        private SimpleHtmlNode([NotNull] HtmlNode node)
        {
            this.myNode = node;
        }

        [NotNull]
        public string Text
        {
            get
            {
                // Fix up dodgy HTML in example text for Unity 5.5
                if (myNode.Name == "pre")
                {
                    var text = "";
                    foreach (var childNode in myNode.ChildNodes)
                    {
                        if (childNode.NodeType == HtmlNodeType.Element && childNode.Name == "br")
                            text += Environment.NewLine;
                        else
                            text += childNode.InnerText;
                    }
                    return text;
                }
                return myNode.InnerText.Trim();
            }
        }

        [CanBeNull]
        public SimpleHtmlNode this[int index] => Wrap(myNode.ChildNodes[index]);

        [NotNull]
        public string this[[NotNull] string attributeName] => myNode.GetAttributeValue(attributeName, string.Empty);

        [CanBeNull]
        public static SimpleHtmlNode Load([NotNull] string path)
        {
            if (!File.Exists(path)) return null;
            var document = new HtmlDocument();
            document.Load(path);
            return new SimpleHtmlNode(document.DocumentNode);
        }

        [NotNull]
        public static SimpleHtmlNode LoadContent(string content)
        {
            var document = new HtmlDocument();
            document.LoadHtml(content);
            return new SimpleHtmlNode(document.DocumentNode);
        }

        [NotNull]
        public SimpleHtmlNode[] SelectMany([NotNull] string xpath)
        {
            var nodes = myNode.SelectNodes(XPath.Resolve(xpath));
            return nodes?.Select(Wrap).ToArray() ?? EmptyArray<SimpleHtmlNode>.Instance;
        }

        [CanBeNull]
        public SimpleHtmlNode SelectOne([NotNull] string xpath)
        {
            return Wrap(myNode.SelectSingleNode(XPath.Resolve(xpath)));
        }

        [NotNull]
        public IEnumerable<SimpleHtmlNode> Subsection([NotNull] string name)
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
            return myNode.InnerHtml;
        }

        [CanBeNull]
        private static SimpleHtmlNode Wrap([CanBeNull] HtmlNode node)
        {
            return node != null ? new SimpleHtmlNode(node) : null;
        }
    }
}
