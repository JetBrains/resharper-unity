using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit
{
    [PolymorphicMarshaller]
    public class AnimImplicitUsagesDataElement: IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as AnimImplicitUsagesDataElement);

        [NotNull, ItemNotNull] public readonly List<AnimImplicitUsage> Events;

        public AnimImplicitUsagesDataElement()
        {
            Events = new List<AnimImplicitUsage>();
        }
        
        public AnimImplicitUsagesDataElement([NotNull] List<AnimImplicitUsage> events)
        {
            Events = events;
        }

        public string ContainerId => nameof(AnimImplicitUsagesContainer);

        public void AddData(object data)
        {
            var usages = (LocalList<AnimImplicitUsage>)data;
            foreach (var usage in usages)
            {
                if (usage is null) continue;
                Events.Add(usage);
            }
        }

        private static object Read([NotNull] UnsafeReader reader)
        {
            var eventUsagesCount = reader.ReadInt32();
            var eventUsages = new List<AnimImplicitUsage>();
            for (var i = 0; i < eventUsagesCount; i++) 
                eventUsages.Add(AnimImplicitUsage.ReadFrom(reader));
            return new AnimImplicitUsagesDataElement(eventUsages);
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] AnimImplicitUsagesDataElement element)
        {
            if (writer is null || element is null) return;
            var events = element.Events;
            writer.Write(events.Count);
            foreach (var usage in events) usage.WriteTo(writer);
        }
    }
}