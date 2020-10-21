using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Maths;

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
            var records = ExtractRecords();
            var stateMachineName = ExtractAnimatorStateNameFrom(records);
            var stateMachineBehavioursAnchors = TryExtractStateMachineBehavioursAnchorsFrom(records);
            var childStateMachinesAnchors = TryExtractChildStateMachinesAnchorsFrom(records);
            var childStatesAnchors = TryExtractChildStatesAnchorsFrom(records);
            AssertHaveUsefulRecords(stateMachineBehavioursAnchors.Count, childStateMachinesAnchors.Count,
                childStatesAnchors.Count);
            return new AnimatorStateMachineScriptUsage(referenceToAnimatorState, stateMachineName,
                stateMachineBehavioursAnchors, childStateMachinesAnchors, childStatesAnchors);
        }

        [AssertionMethod]
        private static void AssertHaveUsefulRecords(int stateMachineBehavioursCount,
                                                    int childStateMachinesAnchorsCount,
                                                    int childStatesAnchorsCount)
        {
            if (stateMachineBehavioursCount + childStateMachinesAnchorsCount + childStatesAnchorsCount == 0)
                throw new AnimatorExtractorException();
        }

        private static LocalList<long> TryExtractChildStatesAnchorsFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            try
            {
                return ExtractChildStatesAnchorsFrom(records);
            }
            catch (AnimatorExtractorException)
            {
            }

            return new LocalList<long>();
        }

        private static LocalList<long> ExtractAnchorsFrom(
            [NotNull] [ItemNotNull] in IEnumerable<IFlowMapEntry> childEntries)
        {
            var childStatesAnchors = new LocalList<long>();
            foreach (var childEntry in childEntries)
            {
                if (!long.TryParse(childEntry.Value?.GetText(), out var anchor)) continue;
                childStatesAnchors.Add(anchor);
            }

            return childStatesAnchors;
        }

        [CanBeNull]
        [ItemNotNull]
        private static IEnumerable<IFlowMapEntry> ExtractChildRecords(
            [ItemNotNull] in TreeNodeCollection<ISequenceEntry> childStatesRecords,
            [NotNull] Func<IBlockMappingEntry, bool> condition)
        {
            var flowMapEntries =
                from childStateRecord in childStatesRecords
                select childStateRecord?.Value as IBlockMappingNode
                into blockMappingNode
                where !(blockMappingNode is null)
                select ExtractRecordFrom<IFlowMappingNode>(blockMappingNode.Entries, condition)
                into stateRecord
                select stateRecord.Entries
                into stateRecordEntries
                where stateRecordEntries.Count == 1
                select stateRecordEntries[0]
                into flowMapEntry
                where !(flowMapEntry is null)
                select flowMapEntry;
            return flowMapEntries;
        }

        private static bool IsChildStatesRecord([NotNull] IBlockMappingEntry record)
        {
            return IsRecord(record, UnityYamlConstants.ChildStatesProperty);
        }

        private static bool IsRecord([NotNull] IBlockMappingEntry record, [NotNull] string propertyName)
        {
            return record.Key.MatchesPlainScalarText(propertyName);
        }

        private static bool IsStateRecord([NotNull] IBlockMappingEntry record)
        {
            return IsRecord(record, UnityYamlConstants.StateProperty);
        }

        private static bool IsStateMachineRecord([NotNull] IBlockMappingEntry record)
        {
            return IsRecord(record, UnityYamlConstants.StateMachineProperty);
        }

        private static LocalList<long> TryExtractStateMachineBehavioursAnchorsFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            try
            {
                return ExtractStateMachineBehavioursAnchorsFrom(records);
            }
            catch (AnimatorExtractorException)
            {
            }

            return new LocalList<long>();
        }

        private static LocalList<long> TryExtractChildStateMachinesAnchorsFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            try
            {
                return ExtractChildStateMachinesAnchorsFrom(records);
            }
            catch (AnimatorExtractorException)
            {
            }

            return new LocalList<long>();
        }

        private static LocalList<long> ExtractChildStatesAnchorsFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            return ExtractChildAnchors(records, IsChildStatesRecord, IsStateRecord);
        }

        private static LocalList<long> ExtractChildStateMachinesAnchorsFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            return ExtractChildAnchors(records, IsChildStateMachinesRecord, IsStateMachineRecord);
        }

        private static LocalList<long> ExtractChildAnchors(TreeNodeCollection<IBlockMappingEntry> records,
                                                           [NotNull]
                                                           Func<IBlockMappingEntry, bool> outerPropertyCondition,
                                                           [NotNull]
                                                           Func<IBlockMappingEntry, bool> nestedPropertyCondition)
        {
            var outerRecords = ExtractRecordFrom<IBlockSequenceNode>(records, outerPropertyCondition).Entries;
            var innerRecords = ExtractChildRecords(outerRecords, nestedPropertyCondition);
            return !(innerRecords is null) ? ExtractAnchorsFrom(innerRecords) : new LocalList<long>();
        }

        private static bool IsChildStateMachinesRecord([NotNull] IBlockMappingEntry record)
        {
            return IsRecord(record, UnityYamlConstants.ChildStateMachinesProperty);
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
            var records = ExtractRecords();
            var animatorStateName = ExtractAnimatorStateNameFrom(records);
            var stateMachineBehavioursAnchors = ExtractStateMachineBehavioursAnchorsFrom(records);
            return new AnimatorStateScriptUsage(referenceToAnimatorState, animatorStateName,
                stateMachineBehavioursAnchors);
        }

        private LocalReference CreateReferenceToAnimatorState()
        {
            var fileStoragePersistentIndex = FindPersistentIndexInPsiStorageOfFile();
            var boxedAnchor = AssetUtils.GetAnchorFromBuffer(myDocument.Buffer);
            if (!boxedAnchor.HasValue) throw new AnimatorExtractorException();
            return new LocalReference(fileStoragePersistentIndex, boxedAnchor.Value);
        }

        private OWORD FindPersistentIndexInPsiStorageOfFile()
        {
            var psiStoragePersistentIndex = myFile
                .PsiStorage
                .PersistentIndex;
            if (psiStoragePersistentIndex is null) throw new AnimatorExtractorException();
            return psiStoragePersistentIndex.Value;
        }

        private TreeNodeCollection<IBlockMappingEntry> ExtractRecords()
        {
            var findRootBlockMapEntries = myDocument.Document.FindRootBlockMapEntries();
            if (findRootBlockMapEntries == null) throw new AnimatorExtractorException();
            return findRootBlockMapEntries.Entries;
        }

        [NotNull]
        private static string ExtractAnimatorStateNameFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            var nameRecord =
                (from record in records
                    let assetDocumentRecordValue = record.Content?.Value
                    where IsNameRecord(record)
                    select assetDocumentRecordValue).FirstOrDefault();
            if (nameRecord is null) throw new AnimatorExtractorException();
            return nameRecord.GetText();
        }

        private static bool IsNameRecord([NotNull] IBlockMappingEntry record)
        {
            return IsRecord(record, UnityYamlConstants.NameProperty);
        }

        private static LocalList<long> ExtractStateMachineBehavioursAnchorsFrom(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records)
        {
            var record = ExtractRecordFrom<IBlockSequenceNode>(records, IsStateMachinesBehavioursRecord);
            return ExtractAnchorsFrom(record.Entries);
        }
        
        [NotNull]
        private static T ExtractRecordFrom<T>(
            [ItemNotNull] in TreeNodeCollection<IBlockMappingEntry> records,
            [NotNull] Func<IBlockMappingEntry, bool> condition) where T : class
        {
            var foundRecord =
                (from record in records
                    let recordValue = record.Content?.Value as T
                    where recordValue != null && condition(record)
                    select recordValue).FirstOrDefault();
            if (foundRecord is null) throw new AnimatorExtractorException();
            return foundRecord;
        }

        private static bool IsStateMachinesBehavioursRecord([NotNull] IBlockMappingEntry record)
        {
            return IsRecord(record, UnityYamlConstants.StateMachineBehavioursProperty);
        }

        private static LocalList<long> ExtractAnchorsFrom(
            [NotNull] [ItemNotNull] in IEnumerable<ISequenceEntry> anchorsRecords)
        {
            return anchorsRecords.Aggregate(new LocalList<long>(), AddAnchor);
        }

        private static LocalList<long> AddAnchor(LocalList<long> anchors, [NotNull] ISequenceEntry record)
        {
            var entry = ExtractEntryFrom(record);
            if (!long.TryParse(entry.Value?.GetText(), out var anchor)) throw new AnimatorExtractorException();
            anchors.Add(anchor);
            return anchors;
        }

        [NotNull]
        private static IFlowMapEntry ExtractEntryFrom([NotNull] ISequenceEntry entry)
        {
            if (!(entry.Value is IFlowMappingNode node)) throw new AnimatorExtractorException();
            return ExtractStateMachineBehaviourEntryFrom(node);
        }

        [NotNull]
        private static IFlowMapEntry ExtractStateMachineBehaviourEntryFrom([NotNull] IFlowMappingNode node)
        {
            var entries = node.Entries;
            if (entries.Count != 1) throw new AnimatorExtractorException();
            return entries[0] ?? throw new AnimatorExtractorException();
        }


        private class AnimatorExtractorException : Exception
        {
        }
    }
}