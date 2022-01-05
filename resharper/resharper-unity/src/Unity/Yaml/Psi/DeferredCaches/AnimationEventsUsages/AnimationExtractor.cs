using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages
{
    internal class AnimationExtractor
    {
        [NotNull] private readonly AssetDocument myDocument;
        [NotNull] private readonly IPsiSourceFile myFile;

        public AnimationExtractor([NotNull] IPsiSourceFile file,
                                  [NotNull] AssetDocument document)
        {
            myFile = file;
            myDocument = document;
        }

        [CanBeNull]
        public List<AnimationUsage> TryExtractEventUsage()
        {
            try
            {
                return ExtractEventUsage();
            }
            catch (AnimationExtractorException)
            {
                return null;
            }
        }

        [NotNull, ItemNotNull]
        private List<AnimationUsage> ExtractEventUsage()
        {
            var root = GetUnityObjectProperties();
            var location = CreateReferenceToAnimationClip();
            var animationName = ExtractAnimationClipNameFrom(root);
            var sampleRate = ExtractSampleRateFrom(root);
            var timesAndNamesAndGuids = ExtractEventsFrom(root);
            var usages = new List<AnimationUsage>();
            foreach (var (time, functionName, guid) in timesAndNamesAndGuids)
            {
                if (functionName is null) continue;
                usages.Add(new AnimationUsage(location, animationName, sampleRate, functionName, time, guid));
            }
            return usages;
        }

        private static int ExtractSampleRateFrom([NotNull] IBlockMappingNode root)
        {
            var sampleRateText = root.GetMapEntryPlainScalarText("m_SampleRate");
            var foundSampleRate = int.TryParse(sampleRateText, out var sampleRate);
            return foundSampleRate ? sampleRate : throw new AnimationExtractorException();
        }

        [NotNull]
        private IBlockMappingNode GetUnityObjectProperties()
        {
            return myDocument.Document.GetUnityObjectProperties() ?? throw new AnimationExtractorException();
        }

        [NotNull]
        private static IEnumerable<Tuple<double, string, Guid>> ExtractEventsFrom([NotNull] in IBlockMappingNode root)
        {
            var events = ExtractAnimationEventsFrom(root);
            var list = new List<Tuple<double, string, Guid>>();
            foreach (var @event in events.Entries) AddEvent(@event, list);
            return list;
        }

        private static void AddEvent([CanBeNull] ISequenceEntry @event,
                                     [NotNull, ItemNotNull] ICollection<Tuple<double, string, Guid>> list)
        {
            if (!(@event?.Value is IBlockMappingNode functionRecord)) return;
            var time = ExtractEventFunctionTimeFrom(functionRecord);
            var functionName = ExtractEventFunctionNameFrom(functionRecord);
            var guid = ExtractEventFunctionGuidFrom(functionRecord);
            if (functionName is null || guid is null) return;
            list.Add(Tuple.Create(time, functionName, guid.Value));
        }

        [NotNull]
        private static string ExtractAnimationClipNameFrom([NotNull] IBlockMappingNode root)
        {
            return root.GetMapEntryPlainScalarText("m_Name") ?? throw new AnimationExtractorException();
        }

        [CanBeNull]
        private static Guid? ExtractEventFunctionGuidFrom([NotNull] IBlockMappingNode record)
        {
            var guidText = record.GetMapEntryValue<IFlowMappingNode>("objectReferenceParameter")
                ?.GetMapEntryPlainScalarText("guid");
            return guidText != null ? new Guid(guidText) : (Guid?) null;
        }

        private static double ExtractEventFunctionTimeFrom([NotNull] IBlockMappingNode record)
        {
            var timeText = record.GetMapEntryPlainScalarText("time");
            return double.TryParse(timeText, out var time) ? time : throw new AnimationExtractorException();
        }

        [CanBeNull]
        private static string ExtractEventFunctionNameFrom([NotNull] IBlockMappingNode record)
        {
            return record.GetMapEntryPlainScalarText("functionName");
        }

        [NotNull]
        private static IBlockSequenceNode ExtractAnimationEventsFrom([NotNull] IBlockMappingNode root)
        {
            return root.GetMapEntryValue<IBlockSequenceNode>("m_Events") ??
                   throw new AnimationExtractorException();
        }

        private LocalReference CreateReferenceToAnimationClip()
        {
            var fileStoragePersistentIndex = FindPersistentIndexInPsiStorageOfFile();
            var boxedAnchor = AssetUtils.GetAnchorFromBuffer(myDocument.Buffer);
            if (!boxedAnchor.HasValue) throw new AnimationExtractorException();
            return new LocalReference(fileStoragePersistentIndex, boxedAnchor.Value);
        }

        private OWORD FindPersistentIndexInPsiStorageOfFile()
        {
            var psiStoragePersistentIndex = myFile.PsiStorage.PersistentIndex;
            if (psiStoragePersistentIndex is null) throw new AnimationExtractorException();
            return psiStoragePersistentIndex.Value;
        }
    }

    internal class AnimationExtractorException : Exception
    {
    }
}