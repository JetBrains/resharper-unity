using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class UnityEventTargetReferenceFactory : IReferenceFactory
    {
        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (ResolveUtil.CheckThatAllReferencesBelongToElement<UnityEventTargetReference>(oldReferences, element))
                return oldReferences;

            if (!(element is IPlainScalarNode methodNameValue))
                return ReferenceCollection.Empty;

            // E.g. element is the m_MethodName scalar value "ButtonClickedHandler" in this structure:
            // m_OnClick:
            //   m_PersistentCalls:
            //     m_Calls:
            //     - m_Target: {fileID: 1870695363}
            //       m_MethodName: ButtonClickedHandler
            //       m_Mode: 3
            //       m_Arguments:
            //         m_ObjectArgument: {fileID: 0}
            //         m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
            //         m_IntArgument: 1
            //         m_FloatArgument: 0
            //         m_StringArgument:
            //         m_BoolArgument: 0
            //       m_CallState: 2
            //   m_TypeName: UnityEngine.UI.Button+ButtonClickedEvent, UnityEngine.UI, Version=1.0.0.0,
            //     Culture=neutral, PublicKeyToken=null
            var methodNameMapEntry = BlockMappingEntryNavigator.GetByValue(methodNameValue);
            var callMapNode = BlockMappingNodeNavigator.GetByEntrie(methodNameMapEntry);
            var callsSequenceEntry = SequenceEntryNavigator.GetByValue(callMapNode);
            var callsSequenceNode = BlockSequenceNodeNavigator.GetByEntrie(callsSequenceEntry);
            var callsMapEntry = BlockMappingEntryNavigator.GetByValue(callsSequenceNode);

            // callsMapEntry should be "m_Calls" (and contain a value that is a sequence node). If it's not null,
            // everything else is also not null
            if (callsMapEntry == null)
                return ReferenceCollection.Empty;

            if (methodNameMapEntry.Key.GetPlainScalarText() == "m_MethodName" &&
                callsMapEntry.Key.GetPlainScalarText() == "m_Calls")
            {
                if (callMapNode.FindChildBySimpleKey("m_Target")?.Value is IFlowMappingNode targetFileIdMappingNode)
                {
                    // I don't think we can get a guid, as a persistent call is defined as a reference to an instance of
                    // UnityObject, so it must exist in the scene. The MonoBehaviour it refers to might be external, tho
                    var guidNode = targetFileIdMappingNode.FindChildBySimpleKey("guid");
                    Assertion.Assert(guidNode == null, "guidNode == null");

                    var fileIdNode = targetFileIdMappingNode.FindChildBySimpleKey("fileID");
                    var fileId = fileIdNode?.Value.GetPlainScalarText();

                    // "0" means null
                    if (fileId != null && fileId != "0")
                    {
                        var reference = new UnityEventTargetReference(methodNameValue, fileId);
                        return new ReferenceCollection(reference);
                    }
                }
            }

            return ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            var methodNameValue = element as IPlainScalarNode;
            var methodNameEntry = BlockMappingEntryNavigator.GetByValue(methodNameValue);
            return methodNameValue != null && methodNameEntry?.Key.GetPlainScalarText() == "m_MethodName" &&
                   names.Contains(methodNameValue.Text.GetText());
        }
    }
}