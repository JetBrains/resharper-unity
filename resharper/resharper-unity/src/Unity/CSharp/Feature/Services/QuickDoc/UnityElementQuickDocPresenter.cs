using System.Xml;
using JetBrains.Application.UI.Help;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Providers;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.OnlineHelp;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    public class UnityElementQuickDocPresenter : IQuickDocPresenter
    {
        private readonly string myDescription;
        private readonly UnityApi myUnityApi;
        private readonly QuickDocTypeMemberProvider myQuickDocTypeMemberProvider;
        private readonly XmlDocHtmlPresenter myXmlDocHtmlPresenter;
        private readonly HelpSystem myHelpSystem;
        private readonly DeclaredElementEnvoy<IClrDeclaredElement> myEnvoy;

        public UnityElementQuickDocPresenter(IClrDeclaredElement element,
                                             string description,
                                             UnityApi unityApi,
                                             QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                             XmlDocHtmlPresenter xmlDocHtmlPresenter,
                                             HelpSystem helpSystem)
        {
            myDescription = description;
            myUnityApi = unityApi;
            myQuickDocTypeMemberProvider = quickDocTypeMemberProvider;
            myXmlDocHtmlPresenter = xmlDocHtmlPresenter;
            myHelpSystem = helpSystem;

            myEnvoy = new DeclaredElementEnvoy<IClrDeclaredElement>(element);
        }

        public QuickDocTitleAndText GetHtml(PsiLanguageType presentationLanguage)
        {
            var elementInstance = myEnvoy.GetValidDeclaredElementInstance();
            var element = elementInstance?.Element as IClrDeclaredElement;
            if (elementInstance == null || element == null) return QuickDocTitleAndText.Empty;

            var details = GetXmlDoc(element);
            var text = myXmlDocHtmlPresenter.Run(details, element.Module,
                elementInstance, presentationLanguage, XmlDocHtmlUtil.NavigationStyle.All,
                XmlDocHtmlUtil.CrefManager);
            var title = DeclaredElementPresenter.Format(presentationLanguage,
                DeclaredElementPresenter.FULL_NESTED_NAME_PRESENTER, element).Text;

            return new QuickDocTitleAndText(text, title);
        }

        private XmlNode GetXmlDoc(IDeclaredElement element)
        {
            var xmlDocNode = element.GetXMLDoc(true);
            if (xmlDocNode == null)
            {
                var memberElement = CreateMemberElement(GetId() ?? string.Empty);
                memberElement.CreateLeafElementWithValue("summary", myDescription);
                xmlDocNode = memberElement;
            }
            else
            {
                // We have XML docs, add our description as an additional node
                ((XmlElement)xmlDocNode).CreateLeafElementWithValue("description", myDescription);
            }

            if (!element.GetPsiServices().Solution.HasComponent<IXmlDocLinkAppender>()) return xmlDocNode;
            var provider = element.GetPsiServices().Solution.GetComponent<UnityOnlineHelpProvider>();
            var uri = provider.GetUrl(element);
            var xmlDocLinkAppender = element.GetPsiServices().Solution.GetComponent<IXmlDocLinkAppender>();
            xmlDocLinkAppender.AppendExternalDocumentationLink(uri, provider.GetPresentableName(element), xmlDocNode);

            return xmlDocNode;
        }

        private static XmlElement CreateMemberElement(string name)
        {
            var xmlElement = new XmlDocument().CreateElement("member");
            xmlElement.SetAttribute("name", name);
            return xmlElement;
        }

        public string? GetId()
        {
            // We don't really use this ID. It gets added to the HTML, but
            // we never navigate to this item, so we never see it again
            var element = myEnvoy.GetValidDeclaredElement();
            if (element != null)
            {
                if (element is IXmlDocIdOwner docOwner)
                    return "Unity:" + docOwner.XMLDocId;
                if (element is IParameter { ContainingParametersOwner: IXmlDocIdOwner xmlDocIdOwner } parameter)
                    return "Unity:" + xmlDocIdOwner.XMLDocId + "#" + parameter.ShortName;
            }
            return null;
        }

        public IQuickDocPresenter? Resolve(string id)
        {
            // Trying to navigate away. The id is the id of the thing we're trying to navigate to. For us, we're
            // navigating away from a Unity event function or parameter, and to a genuine type or type member. The id
            // will be the XML doc ID of the target element and QuickDocTypeMemberProvider will handle it. We'll never
            // get one of our IDs, since we can't navigate to our type member (maybe via a cref)
            var validDeclaredElement = myEnvoy.GetValidDeclaredElement();
            return validDeclaredElement != null
                ? myQuickDocTypeMemberProvider.Resolve(id, myEnvoy.GetValidDeclaredElement()?.Module)
                : null;
        }

        public void OpenInEditor(string navigationId = "")
        {
            var element = myEnvoy.GetValidDeclaredElement();
            element?.Navigate(true);
        }

        public void ReadMore(string navigationId = "")
        {
            // Read more should always navigate to the method, even for parameters
            var element = myEnvoy.GetValidDeclaredElement();
            if (element != null)
            {
                if (element is IParameter parameter)
                    element = parameter.ContainingParametersOwner;

                var unityName = element?.GetUnityEventFunctionName(myUnityApi);
                if (unityName != null)
                    myHelpSystem.ShowHelp(unityName, HelpSystem.HelpKind.Msdn);
            }
        }
    }
}