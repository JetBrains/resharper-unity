using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    public class ProjectSettingsCacheItem
    {
        public class ProjectSettingsSceneData
        {
            public readonly JetHashSet<string> SceneNamesFromBuildSettings;
            public readonly JetHashSet<string> DisabledSceneNamesFromBuildSettings;
            public readonly JetHashSet<string> SceneNames;
            

            public ProjectSettingsSceneData(JetHashSet<string> sceneNamesFromBuildSettings = null,
                JetHashSet<string> disabledSceneNamesFromBuildSettings = null,
                JetHashSet<string> sceneNames = null)
            {
                SceneNamesFromBuildSettings = sceneNamesFromBuildSettings ?? new JetHashSet<string>();
                DisabledSceneNamesFromBuildSettings = disabledSceneNamesFromBuildSettings ?? new JetHashSet<string>();
                SceneNames = sceneNames ?? new JetHashSet<string>();
            }

            public void Write(UnsafeWriter writer)
            {
                WriteSet(writer, SceneNamesFromBuildSettings);
                WriteSet(writer, DisabledSceneNamesFromBuildSettings);
                WriteSet(writer, SceneNames);
            }
            
            public static ProjectSettingsSceneData ReadFrom(UnsafeReader reader)
            {
                return new ProjectSettingsSceneData(ReadSet(reader), ReadSet(reader),
                    ReadSet(reader));
            }

            public bool IsEmpty()
            {
                return SceneNames.IsEmpty() && DisabledSceneNamesFromBuildSettings.IsEmpty() &&
                       SceneNamesFromBuildSettings.IsEmpty();
            }
        }

        public readonly ProjectSettingsSceneData Scenes;
        public readonly JetHashSet<string> Inputs;
        public readonly JetHashSet<string> Tags;
        public readonly JetHashSet<string> Layers;

        public static readonly IUnsafeMarshaller<ProjectSettingsCacheItem> Marshaller =
            new UniversalMarshaller<ProjectSettingsCacheItem>(Read, Write);

        public ProjectSettingsCacheItem(ProjectSettingsSceneData sceneData = null,
            JetHashSet<string> inputs = null,JetHashSet<string> tags = null, JetHashSet<string> layers = null)
        {
            Scenes = sceneData ?? new ProjectSettingsSceneData();
            Inputs = inputs ?? new JetHashSet<string>();
            Tags = tags ?? new JetHashSet<string>();
            Layers = layers ?? new JetHashSet<string>();
        }

        private static ProjectSettingsCacheItem Read(UnsafeReader reader)
        {
            return new ProjectSettingsCacheItem(ProjectSettingsSceneData.ReadFrom(reader),
                ReadSet(reader), ReadSet(reader), ReadSet(reader));
        }

        private static void Write(UnsafeWriter writer, ProjectSettingsCacheItem value)
        {
            value.Scenes.Write(writer);
            WriteSet(writer, value.Inputs);
            WriteSet(writer, value.Tags);
            WriteSet(writer, value.Layers);
        }

        private static void WriteSet(UnsafeWriter writer, JetHashSet<string> list)
        {
            writer.Write(list.Count);
            foreach (var value in list)
            {
                writer.Write(value);
            }
        }

        private static JetHashSet<string> ReadSet(UnsafeReader reader)
        {
            var count = reader.ReadInt32();
            var list = new JetHashSet<string>();
            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }

            return list;
        }

        public bool IsEmpty()
        {
            return Scenes.IsEmpty() &&
                   Tags.IsEmpty() && Inputs.IsEmpty() && Layers.IsEmpty();
        }
    }
}