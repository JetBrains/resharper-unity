using System.Xml;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.QuickDoc;
using JetBrains.ReSharper.Host.Features.Toolset;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    [SolutionComponent]
    public class UnityCompiledElementXmlDocProvider : MonoCompiledElementXmlDocProvider
    {
        private static readonly DeclaredElementPresenterStyle MSDN_STYLE =
            new DeclaredElementPresenterStyle
            {
                ShowEntityKind = EntityKindForm.NONE,
                ShowName = NameStyle.QUALIFIED,
                ShowTypeParameters = TypeParameterStyle.CLR
            };
        
        public UnityCompiledElementXmlDocProvider(RiderApplicationRuntime applicationRuntime, ILogger logger,
            Lifetime lifetime, ISettingsStore settingsStore, ISolution solution, IThreading threading)
            : base(applicationRuntime, logger, lifetime, settingsStore, solution, threading)
        {
        }

        public override XmlNode GetXmlDoc(CompiledElementBase element, bool inherit)
        {
            var result = base.GetXmlDoc(element, inherit);
            if (result == null)
                return null;
            
            var hostName = "docs.unity3d.com";
            if (element is CompiledTypeElement el)
            {
                var moduleName = el.Module.Name;
                if (moduleName.StartsWith("UnityEngine") || moduleName.StartsWith("UnityEditor")) 
                    AddExternalDocumentationLink( $"https://{hostName}/ScriptReference/30_search.html?q={el.ShortName}", hostName, el.ShortName, result);
            }
            else if (element is Member m)
            {
                var moduleName = m.Module.Name;
                if (moduleName.StartsWith("UnityEngine") || moduleName.StartsWith("UnityEditor")) 
                    AddExternalDocumentationLink( $"https://{hostName}/ScriptReference/30_search.html?q={m.ShortName}", hostName, m.ShortName, result);
            }
            return result;
        }
        
        // todo: remove, once sdk one gets public
        private void AddExternalDocumentationLink(string url, string hostName, string shortName,
            XmlNode externalDocNode)
        {
            var document = externalDocNode.OwnerDocument;
            if (document == null)
                return;

            var node = document.CreateNode(XmlNodeType.Element, "a", "");
            var href = document.CreateAttribute("href");
            href.Value = url;
            node.Attributes?.Append(href);
            node.InnerText = $"`{shortName}` on {hostName}";
            externalDocNode.AppendChild(node);
        }
    }
}