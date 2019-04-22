using JetBrains.Collections;
using JetBrains.Serialization;
using JetBrains.Util.Collections;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class ProjectSettingsCacheItem
    {
        public readonly CountingSet<string> SceneNames;
        public readonly CountingSet<string> Inputs;
        public readonly CountingSet<string> Tags;
        public readonly CountingSet<string> Layers;

        public static readonly IUnsafeMarshaller<ProjectSettingsCacheItem> Marshaller =
            new UniversalMarshaller<ProjectSettingsCacheItem>(Read, Write);

        public ProjectSettingsCacheItem(CountingSet<string> sceneNames, CountingSet<string> inputs,
            CountingSet<string> tags, CountingSet<string> layers)
        {
            SceneNames = sceneNames;
            Inputs = inputs;
            Tags = tags;
            Layers = layers;
        }

        public ProjectSettingsCacheItem() : this(new CountingSet<string>(), new CountingSet<string>(),
            new CountingSet<string>(), new CountingSet<string>() )
        {
            
        }

        private static ProjectSettingsCacheItem Read(UnsafeReader reader)
        {
            return new ProjectSettingsCacheItem(ReadCountingSet(reader),
                ReadCountingSet(reader), ReadCountingSet(reader), ReadCountingSet(reader));
        }

        private static void Write(UnsafeWriter writer, ProjectSettingsCacheItem value)
        {
            WriteCountingSet(writer, value.SceneNames);
            WriteCountingSet(writer, value.Inputs);
            WriteCountingSet(writer, value.Tags);
            WriteCountingSet(writer, value.Layers);
        }

        private static void WriteCountingSet(UnsafeWriter writer, CountingSet<string> set)
        {
            writer.Write(set.Count);
            foreach (var (value, count) in set)
            {
                writer.Write(value);
                writer.Write(count);
            }
        }

        private static CountingSet<string> ReadCountingSet(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var set = new CountingSet<string>();
            for (int i = 0; i < count; i++)
            {
                set.Add(reader.ReadString(), reader.ReadInt32());
            }

            return set;
        }
    }
}