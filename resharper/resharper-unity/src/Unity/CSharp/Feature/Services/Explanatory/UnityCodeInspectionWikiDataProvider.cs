using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Explanatory;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Explanatory
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityCodeInspectionWikiDataProvider : ICodeInspectionWikiDataProvider
    {
        private const string WikiRoot = "https://github.com/JetBrains/resharper-unity/wiki/";

        private static readonly IDictionary<string, string> ourUrls =
            new Dictionary<string, string>
            {
                // *****************************************************************************************************
                //
                // CSharpErrors.generated.cs
                //
                // *****************************************************************************************************
                // Undocumented:
                // Static errors
                // * StringLiteralReferenceIncorrectSignatureError
                // * SyncVarUsageError
                //
                // Self explanatory:
                // * UnknownInputAxesWarning
                // * UnknownTagWarning
                // * UnkownLayerWarning
                // * DuplicateShortcutWarning
                // * LoadSceneUnexistingSceneNameWarning
                // * LoadSceneDisabledSceneNameWarning
                // * LoadSceneUnknownSceneNameWarning
                // * LoadSceneAmbiguousSceneNameWarning - maybe?
                // * LoadSceneWrongIndexWarning - maybe?
                // * DuplicateEventFunctionWarning
                // * ExpectedComponentWarning
                // * ExpectedScriptableObjectWarning
                // * IncorrectSignatureWarning
                // * InvalidParametersWarning
                // * InvalidReturnTypeWarning
                // * InvalidStaticModifierWarning
                // * InvalidTypeParametersWarning
                // * ParameterNotDerivedFromComponentWarning
                // * StringLiteralReferenceIncorrectSignatureWarning
                // * UnresolvedComponentOrScriptableObjectWarning
                // * UnityGutterMarkInfo
                // * UnityHotGutterMarkInfo

                { ExplicitTagStringComparisonWarning.HIGHLIGHTING_ID, WikiRoot + "Use-CompareTag-instead-of-explicit-string-comparison" },
                { IncorrectMonoBehaviourInstantiationWarning.HIGHLIGHTING_ID, WikiRoot + "MonoBehaviors-must-be-instantiated-with-GameObject.AddComponent-instead-of-new" },
                { IncorrectScriptableObjectInstantiationWarning.HIGHLIGHTING_ID, WikiRoot + "ScriptableObjects-must-be-instantiated-with-ScriptableObject.CreateInstance-instead-of-new" },
                { InefficientPropertyAccessWarning.HIGHLIGHTING_ID, WikiRoot + "Avoid-multiple-unnecessary-property-accesses" },
                { InstantiateWithoutParentWarning.HIGHLIGHTING_ID, WikiRoot + "Avoid-using-Object.Instantiate-without-“Transform-Parent”-parameter-and-using-SetParent-later" },
                { PossibleMisapplicationOfAttributeToMultipleFieldsWarning.HIGHLIGHTING_ID, WikiRoot + "Possible-mis-application-of-FormerlySerializedAs-attribute-to-multiple-fields" },
                { PreferAddressByIdToGraphicsParamsWarning.HIGHLIGHTING_ID, WikiRoot + "Avoid-using-string-based-names-for-setting-and-getting-properties-on-Animators,-Shaders-and-Materials" },
                { PreferGenericMethodOverloadWarning.HIGHLIGHTING_ID, WikiRoot + "Prefer-using-generic-method-overload-instead-of-string" },
                { PreferNonAllocApiWarning.HIGHLIGHTING_ID, WikiRoot + "Avoid-using-allocating-versions-of-Physics-Raycast-functions" },
                { PropertyDrawerOnGUIBaseWarning.HIGHLIGHTING_ID, WikiRoot + "base.OnGUI()-will-print-%22no-GUI-implemented%22-in-the-Unity-inspector" },
                { RedundantAttributeOnTargetWarning.HIGHLIGHTING_ID, WikiRoot + "Attribute-is-redundant-when-applied-to-this-declaration-type" },
                { RedundantEventFunctionWarning.HIGHLIGHTING_ID, WikiRoot + "Redundant-Unity-event-function" },
                { RedundantFormerlySerializedAsAttributeWarning.HIGHLIGHTING_ID, WikiRoot + "Redundant-FormerlySerializedAs-attribute" },
                { RedundantHideInInspectorAttributeWarning.HIGHLIGHTING_ID, WikiRoot + "Redundant-HideInInspector-attribute" },
                { RedundantInitializeOnLoadAttributeWarning.HIGHLIGHTING_ID, WikiRoot + "Redundant-InitializeOnLoad-attribute" },
                { RedundantSerializeFieldAttributeWarning.HIGHLIGHTING_ID, WikiRoot + "Redundant-SerializeField-attribute" },
                { UnityObjectNullCoalescingWarning.HIGHLIGHTING_ID, WikiRoot + "Possible-unintended-bypass-of-lifetime-check-of-underlying-Unity-engine-object" },
                { UnityObjectNullPropagationWarning.HIGHLIGHTING_ID, WikiRoot + "Possible-unintended-bypass-of-lifetime-check-of-underlying-Unity-engine-object" },
                { UnityObjectNullPatternMatchingWarning.HIGHLIGHTING_ID, WikiRoot + "Possible-unintended-bypass-of-lifetime-check-of-underlying-Unity-engine-object" },

                // *****************************************************************************************************
                //
                // CSharpPerformanceErrors.generated.cs
                //
                // *****************************************************************************************************
                { InefficientMultidimensionalArrayUsageWarning.HIGHLIGHTING_ID, WikiRoot + "Accessing-multidimensional-arrays-is-inefficient" },
                { InefficientMultiplicationOrderWarning.HIGHLIGHTING_ID, WikiRoot + "Order-of-multiplication-operations-is-inefficient" },
                { UnityPerformanceInvocationWarning.HIGHLIGHTING_ID, WikiRoot + "Performance-critical-context-and-costly-methods" },
                { UnityPerformanceNullComparisonWarning.HIGHLIGHTING_ID, WikiRoot + "Avoid-null-comparisons-against-UnityEngine.Object-subclasses" },
                { UnityPerformanceCameraMainWarning.HIGHLIGHTING_ID, WikiRoot + "Camera.main-is-inefficient-in-frequently-called-methods" }
            };

        public bool TryGetValue(string attributeId, out string url)
        {
            return ourUrls.TryGetValue(attributeId, out url);
        }
    }
}
