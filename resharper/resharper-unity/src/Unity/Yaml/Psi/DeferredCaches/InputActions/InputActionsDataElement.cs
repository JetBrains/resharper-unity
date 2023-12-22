using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Serialization;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.InputActions
{
    [PolymorphicMarshaller]
    public class InputActionsDataElement : IUnityAssetDataElement
    {
        [UsedImplicitly] public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        [UsedImplicitly] public static UnsafeWriter.WriteDelegate<object> WriteDelegate =
            (writer, element) => Write(writer, element as InputActionsDataElement);

        [NotNull, ItemNotNull] public readonly List<PlayerInputUsage> Usages;

        public InputActionsDataElement()
        {
            Usages = new List<PlayerInputUsage>();
        }

        public InputActionsDataElement([NotNull] List<PlayerInputUsage> usages)
        {
            Usages = usages;
        }

        public string ContainerId => nameof(InputActionsElementContainer);

        public void AddData(object data)
        {
            var usages = (LocalList<PlayerInputUsage>)data;
            foreach (var usage in usages)
            {
                if (usage is null) continue;
                Usages.Add(usage);
            }
        }

        private static object Read([NotNull] UnsafeReader reader)
        {
            var eventUsagesCount = reader.ReadInt32();
            var eventUsages = new List<PlayerInputUsage>(eventUsagesCount);
            for (var i = 0; i < eventUsagesCount; i++) eventUsages.Add(PlayerInputUsage.ReadFrom(reader));
            return new InputActionsDataElement(eventUsages);
        }

        private static void Write([CanBeNull] UnsafeWriter writer, [CanBeNull] InputActionsDataElement element)
        {
            if (writer is null || element is null) return;
            var eventUsages = element.Usages;
            writer.Write(eventUsages.Count);
            foreach (var eventUsage in eventUsages) eventUsage.WriteTo(writer);
        }
    }
}