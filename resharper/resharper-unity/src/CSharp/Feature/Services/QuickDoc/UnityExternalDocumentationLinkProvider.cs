using System.Xml;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    [SolutionComponent]
    public class UnityExternalDocumentationLinkProvider
    {
        private readonly ShowUnityHelp myShowUnityHelp;

        public UnityExternalDocumentationLinkProvider(ShowUnityHelp showUnityHelp)
        {
            myShowUnityHelp = showUnityHelp;
        }

        public void AddExternalDocumentationLink(XmlNode xmlNode, string keyword)
        {
            var hostName = myShowUnityHelp.HostName;
            var uri = myShowUnityHelp.GetUri(keyword);
            AddExternalDocumentationLink(uri.AbsoluteUri, hostName, keyword, xmlNode);
        }

        // todo: remove, once sdk one gets public `MonoCompiledElementXmlDocProvider.AddExternalDocumentationLink`
        private static void AddExternalDocumentationLink(string url, string hostName, string shortName, XmlNode externalDocNode)
        {
            var summary = externalDocNode.SelectSingleNode("summary");
            if (summary == null)
                return;

            var document = externalDocNode.OwnerDocument;
            if (document == null)
                return;
      
            var para = document.CreateNode(XmlNodeType.Element, "p", "");
            var node = document.CreateNode(XmlNodeType.Element, "a", "");
            var href = document.CreateAttribute("href");
            href.Value = url;
            node.Attributes?.Append(href);
            node.InnerText = $"`{shortName}` on {hostName}";
            para.AppendChild(node);
            summary.AppendChild(para);
        }
    }
}