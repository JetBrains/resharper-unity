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
        private readonly UnityExternalDocumentationLinkProvider myUnityExternalDocumentationLinkProvider;

        public UnityCompiledElementXmlDocProvider(RiderApplicationRuntime applicationRuntime, ILogger logger,
            Lifetime lifetime, ISettingsStore settingsStore, ISolution solution, IThreading threading, UnityExternalDocumentationLinkProvider unityExternalDocumentationLinkProvider)
            : base(applicationRuntime, logger, lifetime, settingsStore, solution, threading)
        {
            myUnityExternalDocumentationLinkProvider = unityExternalDocumentationLinkProvider;
        }

        public override XmlNode GetXmlDoc(CompiledElementBase element, bool inherit)
        {
            var result = base.GetXmlDoc(element, inherit);
            if (result == null)
                return null;

            if (element is IDeclaredElement el)
            {
                var moduleName = element.Module.Name;
                if (moduleName.StartsWith("UnityEngine") || moduleName.StartsWith("UnityEditor"))
                    myUnityExternalDocumentationLinkProvider.AddExternalDocumentationLink(result, el.ShortName);
            }

            return result;
        }
    }
}