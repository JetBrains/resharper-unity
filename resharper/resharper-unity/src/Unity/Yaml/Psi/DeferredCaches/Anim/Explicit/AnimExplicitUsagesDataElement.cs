using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit
{
    [PolymorphicMarshaller]
    public class AnimExplicitUsagesDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as AnimExplicitUsagesDataElement);

        [NotNull, ItemNotNull] public readonly List<AnimExplicitUsage> Events;

        public AnimExplicitUsagesDataElement()
        {
            Events = new List<AnimExplicitUsage>();
        }

        public AnimExplicitUsagesDataElement([NotNull] List<AnimExplicitUsage> events)
        {
            Events = events;
        }

        public string ContainerId => nameof(AnimExplicitUsagesContainer);

        public void AddData(object data)
        {
            var usages = (IEnumerable<AnimExplicitUsage>)data;
            foreach (var usage in usages)
            {
                if (usage is null) continue;
                Events.Add(usage);
            }
        }

        private static object Read([NotNull] UnsafeReader reader)
        {
            var eventUsagesCount = reader.ReadInt32();
            var eventUsages = new List<AnimExplicitUsage>(eventUsagesCount);
            for (var i = 0; i < eventUsagesCount; i++) 
                eventUsages.Add(AnimExplicitUsage.ReadFrom(reader));
            return new AnimExplicitUsagesDataElement(eventUsages);
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] AnimExplicitUsagesDataElement element)
        {
            if (writer is null || element is null) return;
            var eventUsages = element.Events;
            writer.Write(eventUsages.Count);
            foreach (var eventUsage in eventUsages) eventUsage.WriteTo(writer);
        }
    }
}