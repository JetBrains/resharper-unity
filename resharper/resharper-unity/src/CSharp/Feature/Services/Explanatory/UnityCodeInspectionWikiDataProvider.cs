using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Explanatory;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Explanatory
{
    [ShellComponent]
    public class UnityCodeInspectionWikiDataProvider : ICodeInspectionWikiDataProvider
    {
        private static readonly IDictionary<string, string> ourUrls =
            new Dictionary<string, string>
            {
                // "Attribute is redundant when applied to this declaration type"
                {
                    RedundantAttributeOnTargetWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Attribute-is-redundant-when-applied-to-this-declaration-type"
                },
                // "Camera.main is inefficient in frequently called methods"
                {
                    InefficientCameraMainUsageWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Camera.main-is-inefficient-in-frequently-called-methods"
                },
                // "MonoBehaviours must be instantiated with GameObject.AddComponent instead of new"
                {
                    IncorrectMonoBehaviourInstantiationWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/MonoBehaviors-must-be-instantiated-with-GameObject.AddComponent-instead-of-new"
                },
                // "Possible mis-application of 'FormerlySerializedAs' attribute to multiple fields"
                {
                    PossibleMisapplicationOfAttributeToMultipleFieldsWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Possible-mis-application-of-FormerlySerializedAs-attribute-to-multiple-fields"
                },
                // "Possible unintended bypass of lifetime check of underlying Unity engine object"
                {
                    UnityObjectNullCoalescingWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Possible-unintended-bypass-of-lifetime-check-of-underlying-Unity-engine-object"
                },
                // "Possible unintended bypass of lifetime check of underlying Unity engine object"
                {
                    UnityObjectNullPropagationWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Possible-unintended-bypass-of-lifetime-check-of-underlying-Unity-engine-object"
                },
                // "Redundant 'FormerlySerializedAs' attribute"
                {
                    RedundantFormerlySerializedAsAttributeWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Redundant-FormerlySerializedAs-attribute"
                },
                // "Redundant 'InitializeOnLoad' attribute"
                {
                    RedundantInitializeOnLoadAttributeWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Redundant-InitializeOnLoad-attribute"
                },
                // "Redundant 'SerializeField' attribute"
                {
                    RedundantSerializeFieldAttributeWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Redundant-SerializeField-attribute"
                },
                // "Redundant Unity event function"
                {
                    RedundantEventFunctionWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Redundant-Unity-event-function"
                },
                // "ScriptableObjects must be instantiated with ScriptableObject.CreateInstance instead of new"
                {
                    IncorrectScriptableObjectInstantiationWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/ScriptableObjects-must-be-instantiated-with-ScriptableObject.CreateInstance-instead-of-new"
                },
                // "Use CompareTag instead of explicit string comparison"
                {
                    ExplicitTagStringComparisonWarning.HIGHLIGHTING_ID,
                    "https://github.com/JetBrains/resharper-unity/wiki/Use-CompareTag-instead-of-explicit-string-comparison"
                }
            };

        public bool TryGetValue(string attributeId, out string url)
        {
            return ourUrls.TryGetValue(attributeId, out url);
        }
    }
}
