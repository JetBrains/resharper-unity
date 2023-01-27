using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Utils;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Maths;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim
{
    internal class AnimExtractor
    {
        [NotNull] private readonly AssetDocument myDocument;
        [NotNull] private readonly IPsiSourceFile myFile;
        
        private static readonly StringSearcher ourAnimEventsSearcher = new($"  {UnityYamlConstants.EventsProperty}:", false);
        private static readonly StringSearcher ourNameSearcher = new("m_Name", false);
        private static readonly StringSearcher ourSampleRateSearcher = new("m_SampleRate", false);
        
        public AnimExtractor([NotNull] IPsiSourceFile file,
                                  [NotNull] AssetDocument document)
        {
            myFile = file;
            myDocument = document;
        }

        [CanBeNull]
        public List<AnimExplicitUsage> TryExtractEventUsage()
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
        private List<AnimExplicitUsage> ExtractEventUsage()
        {
            var location = CreateReferenceToAnimationClip();
            var animationName = ExtractAnimationClipNameFrom();
            var sampleRate = ExtractSampleRateFrom();
            var timesAndNamesAndGuids = ExtractEvents();
            var usages = new List<AnimExplicitUsage>();
            foreach (var (time, functionName, guid) in timesAndNamesAndGuids)
            {
                if (functionName is null) continue;
                usages.Add(new AnimExplicitUsage(location, animationName, sampleRate, functionName, time, guid));
            }
            return usages;
        }

        private int ExtractSampleRateFrom()
        {
            var sampleRateText = AssetUtils.GetPlainScalarValue(myDocument.Buffer, ourSampleRateSearcher) ??
                                 throw new AnimationExtractorException();
            var foundSampleRate = int.TryParse(sampleRateText, out var sampleRate);
            return foundSampleRate ? sampleRate : throw new AnimationExtractorException();
        }

        [NotNull]
        private IEnumerable<Tuple<double, string, Guid>> ExtractEvents()
        {
            var assetDocument = GetAnimationEventsDocument(myDocument)?? throw new AnimationExtractorException();
            var events = GetAnimationEventsNode(assetDocument) ?? throw new AnimationExtractorException();
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
        private string ExtractAnimationClipNameFrom()
        {
            return AssetUtils.GetPlainScalarValue(myDocument.Buffer, ourNameSearcher) ?? throw new AnimationExtractorException();
        }

        [CanBeNull]
        public static Guid? ExtractEventFunctionGuidFrom([NotNull] IBlockMappingNode record)
        {
            var guidText = record.GetMapEntryValue<IFlowMappingNode>("objectReferenceParameter")
                ?.GetMapEntryPlainScalarText("guid");
            return guidText != null ? new Guid(guidText) : null;
        }

        private static double ExtractEventFunctionTimeFrom([NotNull] IBlockMappingNode record)
        {
            var timeText = record.GetMapEntryPlainScalarText("time");
            return double.TryParse(timeText, out var time) ? time : throw new AnimationExtractorException();
        }

        [CanBeNull]
        public static string ExtractEventFunctionNameFrom([NotNull] IBlockMappingNode record)
        {
            return record.GetMapEntryPlainScalarText("functionName");
        }

        [CanBeNull]
        public static AssetDocument GetAnimationEventsDocument(AssetDocument assetDocument)
        {
            var eventOffset = ourAnimEventsSearcher.Find(assetDocument.Buffer);
            if (eventOffset < 0)
                return null;
            
            var assetDocumentBuffer = assetDocument.Buffer;
            var buffer = ProjectedBuffer.Create(assetDocumentBuffer, new TextRange(eventOffset, assetDocumentBuffer.Length));
            var eventDocument = new AssetDocument(assetDocument.StartOffset + eventOffset, buffer, assetDocument.HierarchyElement);
            return eventDocument;
        }

        [CanBeNull]
        public static IBlockSequenceNode GetAnimationEventsNode(AssetDocument assetDocument)
        {
            var lexer = new YamlLexer(assetDocument.Buffer, false, false);
            var parser = new YamlParser(lexer.ToCachingLexer());
            var document = parser.ParseDocument();

            var eventsNode = (document.Body.BlockNode as IBlockMappingNode).GetMapEntryValue<IBlockSequenceNode>(UnityYamlConstants.EventsProperty);
            return eventsNode;
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