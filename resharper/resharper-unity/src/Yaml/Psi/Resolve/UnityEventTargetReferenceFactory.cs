using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

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
                // If we have a guid, that means this event handler exists inside another asset. That asset might be
                // a .dll, in which case we don't want to add a reference (the primary purpose of these references
                // is to enable Find Usages of methods, not navigation *from* YAML). Or it might be e.g. a prefab.
                // This would be a reference to a prefab that contains a MonoScript asset that has the method
                // TODO: Create an index of other assets that we could target
                var fileID = callMapNode.FindMapEntryBySimpleKey("m_Target")?.Value.AsFileID();
                if (fileID != null && !fileID.IsNullReference && fileID.guid == null)
                {
                    var reference = new UnityEventTargetReference(methodNameValue, fileID);
                    return new ReferenceCollection(reference);
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