using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Descriptions
{
    // Adds the description to the tooltip for an event function and its parameters.
    // Requires "Colour identifiers" and "Replace Visual Studio tooltips" to
    // be checked (or Enhanced Tooltip installed)
    [DeclaredElementDescriptionProvider]
    public class UnityEventFunctionDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        private readonly UnityApi myUnityApi;

        public UnityEventFunctionDescriptionProvider(UnityApi unityApi)
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
                var eventFunction = myUnityApi.GetUnityEventFunction(method);
                if (eventFunction?.Description != null)
                {
                    var richTextBlock = new RichTextBlock(eventFunction.Description);
                    if (eventFunction.Coroutine)
                        richTextBlock.Add("This function can be a coroutine.");
                    return richTextBlock;
                }
            }

            var parameter = element as IParameter;
            var owner = parameter?.ContainingParametersOwner as IMethod;
            if (owner != null)
            {
                var eventFunction = myUnityApi.GetUnityEventFunction(owner);
                var eventFunctionParameter = eventFunction?.GetParameter(parameter.ShortName);
                if (eventFunctionParameter?.Description != null)
                    return new RichTextBlock(eventFunctionParameter.Description);
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