using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimatorUsages
{
    internal class AnimatorExtractor
    {
        [NotNull] private readonly AssetDocument myDocument;
        [NotNull] private readonly IPsiSourceFile myFile;

        public AnimatorExtractor([NotNull] IPsiSourceFile file,
                                 [NotNull] AssetDocument document)
        {
            myFile = file;
            myDocument = document;
        }

        [CanBeNull]
        public AnimatorStateMachineScriptUsage TryExtractStateMachine()
        {
            try
            {
                return ExtractStateMachine();
            }
            catch (AnimatorExtractorException)
            {
                return null;
            }
        }

        [CanBeNull]
        private AnimatorStateMachineScriptUsage ExtractStateMachine()
        {
            var referenceToAnimatorState = CreateReferenceToAnimatorState();
            var root = GetUnityObjectProperties();
            var stateMachineName = ExtractAnimatorStateNameFrom(root);
            var stateMachineBehavioursAnchors = ExtractStateMachineBehavioursAnchorsFrom(root);
            var childStateMachinesAnchors = ExtractChildStateMachinesAnchors(root);
            var childStatesAnchors = ExtractChildStatesAnchors(root);
            AssertHaveUsefulRecords(stateMachineBehavioursAnchors.Count, childStateMachinesAnchors.Count,
                childStatesAnchors.Count);
            return new AnimatorStateMachineScriptUsage(referenceToAnimatorState, stateMachineName,
                stateMachineBehavioursAnchors, childStateMachinesAnchors, childStatesAnchors);
        }

        private static LocalList<long> ExtractChildStatesAnchors([NotNull] IBlockMappingNode root)
        {
            return ExtractChildAnchors(root, "m_ChildStates", "m_State");
        }

        private static LocalList<long> ExtractChildStateMachinesAnchors([NotNull] IBlockMappingNode root)
        {
            return ExtractChildAnchors(root, "m_ChildStateMachines", "m_StateMachine");
        }

        [AssertionMethod]
        private static void AssertHaveUsefulRecords(int stateMachineBehavioursCount,
                                                    int childStateMachinesAnchorsCount,
                                                    int childStatesAnchorsCount)
        {
            if (stateMachineBehavioursCount + childStateMachinesAnchorsCount + childStatesAnchorsCount == 0)
                throw new AnimatorExtractorException();
        }

        private static LocalList<long> ExtractChildAnchors(
            [NotNull] IBlockMappingNode root, string recordKey, string innerRecordKey)
        {
            var anchors = root.GetMapEntryValue<IBlockSequenceNode>(recordKey)?.Entries
                .SelectNotNull(t => t?.Value as IBlockMappingNode)
                .SelectNotNull(t => t.GetMapEntryValue<IFlowMappingNode>(innerRecordKey))
                .SelectNotNull(t => t.GetMapEntryPlainScalarText("fileID"))
                .SelectNotNull(t => long.TryParse(t, out var anchor) ? anchor : (long?) null);
            var list = new LocalList<long>();
            if (anchors is null) return list;
            foreach (var anchor in anchors) list.Add(anchor);
            return list;
        }

        [CanBeNull]
        public AnimatorStateScriptUsage TryExtractUsage()
        {
            try
            {
                return ExtractUsage();
            }
            catch (AnimatorExtractorException)
            {
                return null;
            }
        }

        private AnimatorStateScriptUsage ExtractUsage()
        {
            var referenceToAnimatorState = CreateReferenceToAnimatorState();
            var root = GetUnityObjectProperties();
            var animatorStateName = ExtractAnimatorStateNameFrom(root);
            var stateMachineBehavioursAnchors = ExtractStateMachineBehavioursAnchorsFrom(root);
            return new AnimatorStateScriptUsage(referenceToAnimatorState, animatorStateName,
                stateMachineBehavioursAnchors);
        }

        private LocalReference CreateReferenceToAnimatorState()
        {
            var fileStoragePersistentIndex = myFile.PsiStorage.PersistentIndex.NotNull();
            var boxedAnchor = AssetUtils.GetAnchorFromBuffer(myDocument.Buffer);
            if (!boxedAnchor.HasValue) throw new AnimatorExtractorException();
            return new LocalReference(fileStoragePersistentIndex, boxedAnchor.Value);
        }

        [NotNull]
        private IBlockMappingNode GetUnityObjectProperties()
        {
            return myDocument.Document.GetUnityObjectProperties() ?? throw new AnimatorExtractorException();
        }

        [NotNull]
        private static string ExtractAnimatorStateNameFrom([NotNull] IBlockMappingNode root)
        {
            return root.GetMapEntryPlainScalarText("m_Name") ?? throw new AnimatorExtractorException();
        }

        private static LocalList<long> ExtractStateMachineBehavioursAnchorsFrom([NotNull] IBlockMappingNode root)
        {
            var node = root.GetMapEntryValue<IBlockSequenceNode>("m_StateMachineBehaviours");
            return node?.Entries.Aggregate(new LocalList<long>(), AddAnchor) ?? new LocalList<long>();
        }

        private static LocalList<long> AddAnchor(LocalList<long> anchors, [NotNull] ISequenceEntry record)
        {
            if (!(record.Value is IFlowMappingNode node)) throw new AnimatorExtractorException();
            var entries = node.Entries;
            if (entries.Count != 1 || !long.TryParse(entries[0]?.Value?.GetPlainScalarText(), out var anchor))
                throw new AnimatorExtractorException();
            anchors.Add(anchor);
            return anchors;
        }

        private class AnimatorExtractorException : Exception
        {
        }
    }
}