using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Descriptions
{
    // Adds the description to the tooltip for a message and its parameters.
    // Requires "Colour identifiers" and "Replace Visual Studio tooltips" to
    // be checked (or Enhanced Tooltip installed)
    [DeclaredElementDescriptionProvider]
    public class UnityMessageDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        private readonly UnityApi myUnityApi;

        public UnityMessageDescriptionProvider(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        public RichTextBlock GetElementDescription(IDeclaredElement element, DeclaredElementDescriptionStyle style,
            PsiLanguageType language, IPsiModule module = null)
        {
            if (!element.IsFromUnityProject())
                return null;

            var method = element as IMethod;
            if (method != null)
            {
                var message = myUnityApi.GetUnityMessage(method);
                if (message?.Description != null)
                    return new RichTextBlock(message.Description);
            }

            var parameter = element as IParameter;
            var owner = parameter?.ContainingParametersOwner as IMethod;
            if (owner != null)
            {
                var message = myUnityApi.GetUnityMessage(owner);
                var messageParameter = message?.GetParameter(parameter.ShortName);
                if (messageParameter?.Description != null)
                    return new RichTextBlock(messageParameter.Description);
            }

            return null;
        }

        public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock obsoleteDescription,
            DeclaredElementDescriptionStyle style)
        {
            obsoleteDescription = null;
            return false;
        }

        public int Priority => 10;
    }
}