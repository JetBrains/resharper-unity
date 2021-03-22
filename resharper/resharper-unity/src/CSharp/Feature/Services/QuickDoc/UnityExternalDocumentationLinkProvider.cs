// using System.Xml;
// using JetBrains.ProjectModel;
// using JetBrains.ReSharper.Plugins.Unity.Application.UI.Help;
//
// namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
// {
//     [SolutionComponent]
//     public class UnityExternalDocumentationLinkProvider
//     {
//         private readonly ShowUnityHelp myShowUnityHelp;
//
//         public UnityExternalDocumentationLinkProvider(ShowUnityHelp showUnityHelp)
//         {
//             myShowUnityHelp = showUnityHelp;
//         }
//
//         public void AddExternalDocumentationLink(XmlNode xmlNode, string keyword)
//         {
//             var hostName = myShowUnityHelp.HostName;
//             var uri = myShowUnityHelp.GetUri(keyword);
//             CompiledElementXmlDocLinkManager.AppendExternalDocumentationLink(uri, hostName, keyword, xmlNode);
//         }
//     }
// }