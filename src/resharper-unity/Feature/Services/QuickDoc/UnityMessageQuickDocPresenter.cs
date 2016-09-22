using System.Xml;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Providers;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Application;
using JetBrains.UI.Theming;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickDoc
{
    public class UnityMessageQuickDocPresenter : IQuickDocPresenter
    {
        private readonly UnityMessage myMessage;
        private readonly string myParameterName;
        private readonly QuickDocTypeMemberProvider myQuickDocTypeMemberProvider;
        private readonly ITheming myTheming;
        private readonly HelpSystem myHelpSystem;
        private readonly DeclaredElementEnvoy<IClrDeclaredElement> myEnvoy;

        private readonly DeclaredElementPresenterStyle myMsdnStyle =
            new DeclaredElementPresenterStyle
            {
                ShowEntityKind = EntityKindForm.NONE,
                ShowName = NameStyle.QUALIFIED,
                ShowTypeParameters = TypeParameterStyle.CLR
            };

        public UnityMessageQuickDocPresenter(UnityMessage message, IClrDeclaredElement element,
                                             QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                             ITheming theming, HelpSystem helpSystem)
            : this(message, null, element, quickDocTypeMemberProvider, theming, helpSystem)
        {
            myQuickDocTypeMemberProvider = quickDocTypeMemberProvider;
        }

        public UnityMessageQuickDocPresenter(UnityMessage message, string parameterName,
                                             IClrDeclaredElement element,
                                             QuickDocTypeMemberProvider quickDocTypeMemberProvider,
                                             ITheming theming, HelpSystem helpSystem)
        {
            myMessage = message;
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
                XmlDocHtmlUtil.ProcessCRef, myTheming);
            var title = DeclaredElementPresenter.Format(presentationLanguage,
                DeclaredElementPresenter.FULL_NESTED_NAME_PRESENTER, element);

            return new QuickDocTitleAndText(text, title);
        }

        private XmlNode GetDetails(IDeclaredElement element)
        {
            var xmlDocNode = element.GetXMLDoc(true);
            if (xmlDocNode != null)
                return xmlDocNode;

            var description = myMessage.Description;
            if (!string.IsNullOrWhiteSpace(myParameterName))
                description = myMessage.GetParameter(myParameterName)?.Description;

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
            // a Unity message or message parameter, and to a genuine type
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
                    var unityName = myMessage.TypeName + "." + element.ShortName;
                    myHelpSystem.ShowHelp(unityName, HelpSystem.HelpKind.Msdn);
                }
            }
        }
    }
}