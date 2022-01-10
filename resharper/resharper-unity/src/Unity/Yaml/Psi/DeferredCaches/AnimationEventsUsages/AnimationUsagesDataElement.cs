using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AnimationEventsUsages
{
    [PolymorphicMarshaller]
    public class AnimationUsagesDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as AnimationUsagesDataElement);

        [NotNull, ItemNotNull] public readonly List<AnimationUsage> Events;

        public AnimationUsagesDataElement([NotNull] ISolution solution, [NotNull] MetaFileGuidCache metaFileGuidCache)
        {
            Events = new List<AnimationUsage>();
        }

        public AnimationUsagesDataElement([NotNull] List<AnimationUsage> events)
        {
            Events = events;
        }

        public string ContainerId => nameof(AnimationEventUsagesContainer);

        public void AddData(object data)
        {
            if (!(data is IEnumerable<AnimationUsage> usages)) return;
            foreach (var usage in usages)
            {
                if (usage is null) continue;
                Events.Add(usage);
            }
        }

        private static object Read([NotNull] UnsafeReader reader)
        {
            var guidToEvents = ReadGuidToEventsMap(reader);
            return new AnimationUsagesDataElement(guidToEvents);
        }

        [NotNull]
        private static List<AnimationUsage> ReadGuidToEventsMap([NotNull] UnsafeReader reader)
        {
            var eventUsagesCount = reader.ReadInt32();
            var eventUsages = new List<AnimationUsage>(eventUsagesCount);
            for (var i = 0; i < eventUsagesCount; i++) eventUsages.Add(AnimationUsage.ReadFrom(reader));
            return eventUsages;
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] AnimationUsagesDataElement element)
        {
            if (writer is null || element is null) return;
            var eventUsages = element.Events;
            writer.Write(eventUsages.Count);
            foreach (var eventUsage in eventUsages) eventUsage.WriteTo(writer);
        }
    }
}