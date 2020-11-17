using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Collections;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages
{
    [PolymorphicMarshaller]
    public class AnimationUsagesDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as AnimationUsagesDataElement);

        [NotNull] public readonly OneToListMap<Pair<string, Guid>, AnimationUsage> FunctionNameAndGuidToEvents;

        public AnimationUsagesDataElement()
        {
            FunctionNameAndGuidToEvents = new OneToListMap<Pair<string, Guid>, AnimationUsage>();
        }

        public AnimationUsagesDataElement([NotNull] OneToListMap<Pair<string, Guid>, AnimationUsage> functionNameAndGuidToEvents)
        {
            FunctionNameAndGuidToEvents = functionNameAndGuidToEvents;
        }

        public string ContainerId => nameof(AnimationEventUsagesContainer);

        public void AddData(object data)
        {
            if (!(data is IEnumerable<AnimationUsage> usages)) return;
            foreach (var usage in usages)
            {
                if (usage is null) continue;
                FunctionNameAndGuidToEvents.Add(Pair.Of(usage.FunctionName, usage.Guid), usage);
            }
        }

        private static object Read([NotNull] UnsafeReader reader)
        {
            var guidToEvents = ReadGuidToEventsMap(reader);
            return new AnimationUsagesDataElement(guidToEvents);
        }

        [NotNull]
        private static OneToListMap<Pair<string, Guid>, AnimationUsage> ReadGuidToEventsMap(
            [NotNull] UnsafeReader reader)
        {
            var anchorToUsagesCount = reader.ReadInt32();
            var anchorToUsages = new OneToListMap<Pair<string, Guid>, AnimationUsage>(anchorToUsagesCount);
            for (var i = 0; i < anchorToUsagesCount; i++) ReadAnchorToUsagesEntry(reader, anchorToUsages);
            return anchorToUsages;
        }

        private static void ReadAnchorToUsagesEntry([NotNull] UnsafeReader reader,
                                                    [NotNull] IOneToManyMap<Pair<string, Guid>, AnimationUsage,
                                                        IList<AnimationUsage>> anchorToUsages)
        {
            var functionName = reader.ReadString();
            var guid = reader.ReadGuid();
            var usagesCount = reader.ReadInt32();
            for (var i = 0; i < usagesCount; i++)
            {
                var animationEventUsage = AnimationUsage.ReadFrom(reader);
                anchorToUsages.Add(Pair.Of(functionName, guid), animationEventUsage);
            }
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] AnimationUsagesDataElement element)
        {
            if (writer is null || element is null) return;
            WriteGuidToEventsMap(writer, element.FunctionNameAndGuidToEvents);
        }

        private static void WriteGuidToEventsMap([NotNull] UnsafeWriter writer,
                                                 [NotNull]
                                                 OneToListMap<Pair<string, Guid>, AnimationUsage> guidToEvents)
        {
            writer.Write(guidToEvents.Count);
            foreach (var (guid, events) in guidToEvents)
            {
                if (events is null) continue;
                WriteGuidToEventsEntry(writer, guid, events);
            }
        }

        private static void WriteGuidToEventsEntry([NotNull] UnsafeWriter writer,
                                                   Pair<string, Guid> functionNameAndGuid,
                                                   [NotNull] IList<AnimationUsage> events)
        {
            var (functionName, guid) = functionNameAndGuid;
            writer.Write(functionName);
            writer.Write(guid);
            writer.Write(events.Count);
            foreach (var @event in events)
            {
                if (@event is null) return;
                @event.WriteTo(writer);
            }
        }
    }
}