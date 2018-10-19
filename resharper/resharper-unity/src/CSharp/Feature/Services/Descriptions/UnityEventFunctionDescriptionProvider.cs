using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Descriptions
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
            UnityEventFunction eventFunction;

            if (element is IMethod method && (method.GetContainingType()?.IsFromUnityProject() ?? false))
            {
                eventFunction = myUnityApi.GetUnityEventFunction(method);
                if (eventFunction?.Description != null)
                {
                    var richTextBlock = new RichTextBlock(eventFunction.Description);
                    if (eventFunction.Coroutine)
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

        public int Priority => 10;
    }
}