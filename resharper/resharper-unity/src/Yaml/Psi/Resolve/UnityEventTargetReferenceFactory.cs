using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve
{
    public class UnityEventTargetReferenceFactory : IReferenceFactory
    {
        private static readonly StringSearcher ourMethodNameSearcher = new StringSearcher("m_MethodName:", true);

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
            var methodNameMapEntry = BlockMappingEntryNavigator.GetByContent(ContentNodeNavigator.GetByValue(methodNameValue));
            var callMapNode = BlockMappingNodeNavigator.GetByEntrie(methodNameMapEntry);
            var callsMapEntry = BlockMappingEntryNavigator.GetByContent(
                ContentNodeNavigator.GetByValue(
                    BlockSequenceNodeNavigator.GetByEntrie(SequenceEntryNavigator.GetByValue(callMapNode))));

            // callsMapEntry should be "m_Calls" (and contain a value that is a sequence node). If it's not null,
            // everything else is also not null
            if (callsMapEntry == null)
                return ReferenceCollection.Empty;

            var persistentCallsMapNode = BlockMappingNodeNavigator.GetByEntrie(
                BlockMappingEntryNavigator.GetByContent(
                    ContentNodeNavigator.GetByValue(BlockMappingNodeNavigator.GetByEntrie(callsMapEntry))));
            var eventTypeName = persistentCallsMapNode?.FindMapEntryBySimpleKey("m_TypeName")?.Content?.Value
                ?.GetPlainScalarText();

            if (methodNameMapEntry.Key.MatchesPlainScalarText("m_MethodName") &&
                callsMapEntry.Key.MatchesPlainScalarText("m_Calls") && eventTypeName != null)
            {
                var fileID = callMapNode.FindMapEntryBySimpleKey("m_Target")?.Content.Value.AsFileID();
                if (fileID != null && !fileID.IsNullReference)
                {
                    var modeText = callMapNode.FindMapEntryBySimpleKey("m_Mode")?.Content.Value.GetPlainScalarText();

                    var argMode = EventHandlerArgumentMode.EventDefined;
                    if (int.TryParse(modeText, out var mode))
                    {
                        if (1 <= mode && mode <= 6)
                            argMode = (EventHandlerArgumentMode) mode;
                    }

                    var arguments = callMapNode.FindMapEntryBySimpleKey("m_Arguments")?.Content.Value as IBlockMappingNode;
                    var argumentTypeName = arguments.FindMapEntryBySimpleKey("m_ObjectArgumentAssemblyTypeName")?.Content
                        .Value.GetPlainScalarText();
                    var type = argumentTypeName?.Split(',').FirstOrDefault();
                    if (argMode == EventHandlerArgumentMode.EventDefined)
                        type = eventTypeName.Split(',').FirstOrDefault();
                    else if (argMode == EventHandlerArgumentMode.Void)
                        type = null;

                    var reference = new UnityEventTargetReference(methodNameValue, argMode, type, fileID);
                    return new ReferenceCollection(reference);
                }
            }

            return ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            if (element is IPlainScalarNode methodNameValue && CanHaveReference(methodNameValue))
                return names.Contains(methodNameValue.Text.GetText());
            return false;
        }

        public static bool CanContainReference([NotNull] IYamlDocument document)
        {
            // This document can only contain a reference if it represents a MonoBehaviour (which includes compiled
            // MonoBehaviours such as Button) and if it has the `m_MethodName` property. So, check the text of the
            // closed chameleon for "!u!114" and "m_MethodName".
            // TODO: Can we improve this?
            // When the chameleon is closed, GetTextAsBuffer returns a ProjectedBuffer over the source file element.
            // When open, it's a bit more expensive, by creating a StringBuffer over the result of GetText, which is
            // calculated by pre-initialising a StringBuilder to the correct length and calling GetText(StringBuilder)
            // on the child nodes.
            // Then we search the buffer, potentially twice. We'll limit the tag searcher to the first 100 characters of
            // the buffer
            var buffer = document.GetTextAsBuffer();
            return CanContainReference(buffer);
        }

        public static bool CanContainReference(IBuffer bodyBuffer)
        {
            return ourMethodNameSearcher.Find(bodyBuffer) >= 0;
        }

        public static bool CanHaveReference([CanBeNull] ITreeNode element)
        {
            var methodNameValue = element as IPlainScalarNode;
            var methodNameEntry = BlockMappingEntryNavigator.GetByContent(ContentNodeNavigator.GetByValue(methodNameValue));
            return methodNameValue != null && (methodNameEntry?.Key.MatchesPlainScalarText("m_MethodName") ?? false);
        }
    }
}