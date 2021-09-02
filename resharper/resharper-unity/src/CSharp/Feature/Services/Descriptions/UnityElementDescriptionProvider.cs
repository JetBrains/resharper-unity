using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Descriptions
{
    // Provides a description for various Unity related declared elements, such as event function descriptions. Used in
    // various places, usually as a fallback if there is no XML documentation. Can be seen when e.g. generating event
    // functions (provides the descriptions for the dialog or code completion). Also provides the descriptions in
    // QuickDoc, but only because of a custom QuickDoc provider.
    [DeclaredElementDescriptionProvider]
    public class UnityElementDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        private readonly UnityApi myUnityApi;

        public UnityElementDescriptionProvider(UnityApi unityApi)
        {
            myUnityApi = unityApi;
        }

        // Higher than CLrDeclaredElementXmlDescriptionProvider, so XML doc comments take precedence.
        public int Priority => 10;

        public RichTextBlock GetElementDescription(IDeclaredElement element,
                                                   DeclaredElementDescriptionStyle style,
                                                   PsiLanguageType language,
                                                   IPsiModule module = null)
        {
            UnityEventFunction eventFunction;

            if (element is IMethod method && (method.GetContainingType()?.IsFromUnityProject() ?? false))
            {
                eventFunction = myUnityApi.GetUnityEventFunction(method);
                if (eventFunction?.Description != null)
                {
                    var richTextBlock = new RichTextBlock(eventFunction.Description);
                    if (eventFunction.CanBeCoroutine)
                        richTextBlock.Add("This function can be a coroutine.");
                    if (eventFunction.Undocumented)
                        richTextBlock.Add("This function is undocumented.");
                    return richTextBlock;
                }
            }

            var parameter = element as IParameter;
            var owner = parameter?.ContainingParametersOwner as IMethod;
            if (owner == null || owner.GetContainingType()?.IsFromUnityProject() == false)
                return null;

            eventFunction = myUnityApi.GetUnityEventFunction(owner, out var match);
            if (eventFunction == null || (match & MethodSignatureMatch.IncorrectParameters) ==
                MethodSignatureMatch.IncorrectParameters)
            {
                return null;
            }

            var eventFunctionParameter = eventFunction.GetParameter(parameter.ShortName);
            if (eventFunctionParameter == null)
            {
                var parameters = parameter.ContainingParametersOwner.Parameters;
                for (var i = 0; i < parameters.Count; i++)
                {
                    if (Equals(parameters[i], parameter))
                    {
                        eventFunctionParameter = eventFunction.Parameters[i];
                        break;
                    }
                }
            }

            if (eventFunctionParameter?.Description != null)
            {
                var richTextBlock = new RichTextBlock(eventFunctionParameter.Description);
                if (eventFunctionParameter.IsOptional)
                {
                    if (string.IsNullOrEmpty(eventFunctionParameter.Justification))
                        richTextBlock.Add("This parameter is optional and can be removed if not used.");
                    else
                        richTextBlock.Add($"This parameter is optional: {eventFunctionParameter.Justification}");
                }

                return richTextBlock;
            }

            return null;
        }

        public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock obsoleteDescription,
                                       DeclaredElementDescriptionStyle style)
        {
            obsoleteDescription = null;
            return false;
        }
    }
}