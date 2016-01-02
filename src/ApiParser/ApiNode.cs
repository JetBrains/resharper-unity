using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using JetBrains.Annotations;

namespace ApiParser
{
    public sealed class ApiNode
    {
        private readonly HtmlNode _node;

        private ApiNode([NotNull] HtmlNode node)
        {
            _node = node;
        }

        [NotNull]
        public string Code => _node.InnerHtml;

        [NotNull]
        public string Text => _node.InnerText.Trim();

        [CanBeNull]
        public ApiNode this[int index] => Wrap(_node.ChildNodes[index]);

        [NotNull]
        public string this[[NotNull] string attributeName] => _node.GetAttributeValue(attributeName, string.Empty);

        [CanBeNull]
        public static ApiNode Load([NotNull] string path)
        {
            if (!File.Exists(path)) return null;
            var document = new HtmlDocument();
            document.Load(path);
            return new ApiNode(document.DocumentNode);
        }

        [NotNull]
        public IEnumerable<ApiNode> SelectMany([NotNull] string xpath)
        {
            HtmlNodeCollection nodes = _node.SelectNodes(XPath.Resolve(xpath));
            return nodes?.Select(Wrap) ?? new ApiNode[ 0 ];
        }

        [CanBeNull]
        public ApiNode SelectOne([NotNull] string xpath)
        {
            return Wrap(_node.SelectSingleNode(XPath.Resolve(xpath)));
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
