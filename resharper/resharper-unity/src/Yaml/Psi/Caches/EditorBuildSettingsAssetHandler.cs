using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class EditorBuildSettingsAssetHandler : IProjectSettingsAssetHandler
    {
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("EditorBuildSettings.asset");
        }

        public void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem)
        {
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            var scenesArray = GetSceneCollection(file);
            Assertion.Assert(scenesArray != null, "scenesArray != null");

            if (scenesArray is IBlockSequenceNode node)
            {
                foreach (var s in node.Entries)
                {
                    var scene = s.Value;
                    var sceneRecord = scene as IBlockMappingNode;
                    if (sceneRecord == null)
                        continue;
                    
                    var path = GetUnityScenePathRepresentation((sceneRecord.Entries[1].Value as IPlainScalarNode)?.Text.GetText());
                    var isEnabledPlaneScalarNode = (sceneRecord.Entries[0].Value as IPlainScalarNode);
                    var isEnabled = isEnabledPlaneScalarNode?.Text.GetText().Equals("1");
                    if (path == null || !isEnabled.HasValue)
                        continue;
                    
                    if (isEnabled.Value)
                    {
                        cacheItem.Scenes.SceneNamesFromBuildSettings.Add(path);
                    }
                    else
                    {
                        cacheItem.Scenes.DisabledSceneNamesFromBuildSettings.Add(path);
                    }
                }
            }
        }
    }
}