using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Descriptions
{
    // Provides a description for various Unity related declared elements, such as event function descriptions. Used in
    // various places, usually as a fallback if there is no XML documentation. Can be seen when e.g. generating event
    // functions (provides the descriptions for the dialog or code completion). Also provides the descriptions in
    // QuickDoc, but only because of a custom QuickDoc provider.
    [DeclaredElementDescriptionProvider(Instantiation.DemandAnyThreadSafe)]
    public class UnityElementDescriptionProvider : IDeclaredElementDescriptionProvider
    {
        private readonly UnityApi myUnityApi;
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UnityElementDescriptionProvider(UnityApi unityApi, UnitySolutionTracker unitySolutionTracker)
        {
            myUnityApi = unityApi;
            myUnitySolutionTracker = unitySolutionTracker;
        }

        // Higher than CLrDeclaredElementXmlDescriptionProvider, so XML doc comments take precedence.
        public int Priority => 10;

        public RichTextBlock GetElementDescription(IDeclaredElement element,
                                                   DeclaredElementDescriptionStyle style,
                                                   PsiLanguageType language,
                                                   IPsiModule module = null)
        {
            if (!myUnitySolutionTracker.HasUnityReference.Value) return null;

            return element switch
            {
                IMethod method => GetEventFunctionDescription(method),
                IParameter parameter => GetEventFunctionParameterDescription(parameter),
                IField field => GetSerialisedFieldDescription(field),
                _ => null
            };
        }

        public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock obsoleteDescription,
                                       DeclaredElementDescriptionStyle style)
        {
            obsoleteDescription = null;
            return false;
        }

        [CanBeNull]
        private RichTextBlock GetEventFunctionDescription(IMethod method)
        {
            var eventFunction = myUnityApi.GetUnityEventFunction(method);
            if (eventFunction?.Description != null)
            {
                var richTextBlock = new RichTextBlock(eventFunction.Description);
                if (eventFunction.CanBeCoroutine)
                    richTextBlock.Add(Strings.UnityCommonIconProvider_GetEventFunctionTooltip_This_function_can_be_a_coroutine_);
                if (eventFunction.Undocumented)
                    richTextBlock.Add(Strings.UnityElementDescriptionProvider_GetEventFunctionDescription_This_function_is_undocumented_);
                return richTextBlock;
            }

            return null;
        }

        [CanBeNull]
        private RichTextBlock GetEventFunctionParameterDescription(IParameter parameter)
        {
            if (parameter?.ContainingParametersOwner is not IMethod owner)
                return null;

            var eventFunction = myUnityApi.GetUnityEventFunction(owner, out var match);
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
                    richTextBlock.Add(string.IsNullOrEmpty(eventFunctionParameter.Justification)
                        ? Strings.UnityElementDescriptionProvider_GetEventFunctionParameterDescription_This_parameter_is_optional_and_can_be_removed_if_not_used_
                        : string.Format(Strings.UnityElementDescriptionProvider_GetEventFunctionParameterDescription_This_parameter_is_optional___0_, eventFunctionParameter.Justification));
                }

                return richTextBlock;
            }

            return null;
        }

        [CanBeNull]
        private RichTextBlock GetSerialisedFieldDescription(IField field)
        {
            if (myUnityApi.IsSerialisedField(field).HasFlag(SerializedFieldStatus.NonSerializedField)) return null;

            foreach (var attribute in field.GetAttributeInstances(KnownTypes.TooltipAttribute, AttributesSource.Self))
            {
                if (attribute.PositionParameterCount > 0)
                {
                    var tooltipText = attribute.PositionParameter(0).TryGetString();
                    if (tooltipText != null)
                        return new RichTextBlock(tooltipText);
                }
            }

            return null;
        }
    }
}