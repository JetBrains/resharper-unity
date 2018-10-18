using System.Xml;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.Application.UI.Help;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Providers;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickDoc
{
    public class UnityEventFunctionQuickDocPresenter : IQuickDocPresenter
    {
        private readonly UnityEventFunction myEventFunction;
        private readonly string myParameterName;
        private readonly QuickDocTypeMemberProvider myQuickDocTypeMemberProvider;
        private readonly ITheming myTheming;
        private readonly HelpSystem myHelpSystem;
        private readonly DeclaredElementEnvoy<IClrDeclaredElement> myEnvoy;

        public UnityEventFunctionQuickDocPresenter(UnityEventFunction eventFunction, IClrDeclaredElement element,
                                                   QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                                   ITheming theming, HelpSystem helpSystem)
            : this(eventFunction, null, element, quickDocTypeMemberProvider, theming, helpSystem)
        {
            myQuickDocTypeMemberProvider = quickDocTypeMemberProvider;
        }

        public UnityEventFunctionQuickDocPresenter(UnityEventFunction eventFunction, string parameterName,
                                                   IClrDeclaredElement element,
                                                   QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                                   ITheming theming, HelpSystem helpSystem)
        {
            myEventFunction = eventFunction;
            myParameterName = parameterName;
            myQuickDocTypeMemberProvider = quickDocTypeMemberProvider;
            myTheming = theming;
            myHelpSystem = helpSystem;
            myEnvoy = new DeclaredElementEnvoy<IClrDeclaredElement>(element);
        }

        public QuickDocTitleAndText GetHtml(PsiLanguageType presentationLanguage)
        {
            var element = myEnvoy.GetValidDeclaredElement();
            if (element == null) return QuickDocTitleAndText.Empty;

            // Present in the standard fashion
            var details = GetDetails(element);
            var text = XmlDocHtmlPresenter.Run(details, element.Module,
                element, presentationLanguage, XmlDocHtmlUtil.NavigationStyle.All,
                XmlDocHtmlUtil.CrefManager, myTheming);
            var title = DeclaredElementPresenter.Format(presentationLanguage,
                DeclaredElementPresenter.FULL_NESTED_NAME_PRESENTER, element);

            return new QuickDocTitleAndText(text, title);
        }

        private XmlNode GetDetails(IDeclaredElement element)
        {
            var xmlDocNode = element.GetXMLDoc(true);
            if (xmlDocNode != null)
                return xmlDocNode;

            var description = myEventFunction.Description;
            if (!string.IsNullOrWhiteSpace(myParameterName))
                description = myEventFunction.GetParameter(myParameterName)?.Description;

            var details = CreateMemberElement(element);
            if (!string.IsNullOrWhiteSpace(description))
                details.CreateLeafElementWithValue("summary", description);
            return details;
        }

        private XmlElement CreateMemberElement(IDeclaredElement element)
        {
            var xmlElement = new XmlDocument().CreateElement("member");
            xmlElement.SetAttribute("name", element == null ? string.Empty : GetId());
            return xmlElement;
        }

        public string GetId()
        {
            // We don't really use this ID. It gets added to the HTML, but
            // we never navigate to this item, so we never see it again
            var element = myEnvoy.GetValidDeclaredElement();
            if (element != null)
            {
                var docOwner = element as IXmlDocIdOwner;
                if (docOwner != null)
                    return "Unity:" + docOwner.XMLDocId;
                var parameter = element as IParameter;
                if (parameter != null)
                {
                    docOwner = parameter.ContainingParametersOwner as IXmlDocIdOwner;
                    if (docOwner != null)
                        return "Unity:" + docOwner.XMLDocId + "#" + parameter.ShortName;
                }
            }
            return null;
        }

        public IQuickDocPresenter Resolve(string id)
        {
            // Trying to navigate away. The id is the id of the thing we're
            // trying to navigate to. For us, we're navigating away from
            // a Unity event function or parameter, and to a genuine type
            // or type member. The id will be the XML doc ID of the target
            // element and QuickDocTypeMemberProvider will handle it. We'll
            // never get one of our IDs, since we can't navigate to our
            // type member (maybe via a cref)
            var validDeclaredElement = myEnvoy.GetValidDeclaredElement();
            if (validDeclaredElement != null)
            {
                return myQuickDocTypeMemberProvider.Resolve(id, myEnvoy.GetValidDeclaredElement()?.Module);
            }
            return null;
        }

        public void OpenInEditor()
        {
            var element = myEnvoy.GetValidDeclaredElement();
            element?.Navigate(true);
        }

        public void ReadMore()
        {
            // Read more should always navigate to the method, even for parameters
            var element = myEnvoy.GetValidDeclaredElement();
            if (element != null)
            {
                var parameter = element as IParameter;
                if (parameter != null)
                    element = parameter.ContainingParametersOwner;

                if (element != null)
                {
                    // TODO: Is there a nice helper for this?
                    var unityName = myEventFunction.TypeName + "." + element.ShortName;
                    myHelpSystem.ShowHelp(unityName, HelpSystem.HelpKind.Msdn);
                }
            }
        }
    }
}