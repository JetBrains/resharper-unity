using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityProjectSettingsUtils;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class EditorBuildSettingsAssetHandler : IProjectSettingsAssetHandler
    {
        private readonly ILogger myLogger;

        public EditorBuildSettingsAssetHandler(ILogger logger)
        {
            myLogger = logger;
        }
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("EditorBuildSettings.asset");
        }

        public void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem)
        {
            var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IYamlFile;
            var scenesArray = GetSceneCollection(file);

            if (scenesArray == null)
            {
                myLogger.Error("scenesArray != null");
                return;
            }
            
            if (scenesArray is IBlockSequenceNode node)
            {
                foreach (var s in node.Entries)
                {
                    var scene = s.Value;
                    var sceneRecord = scene as IBlockMappingNode;
                    if (sceneRecord == null)
                        continue;
                    
                    var path = GetUnityScenePathRepresentation(sceneRecord.Entries[1].Content.Value.GetPlainScalarText());
                    var isEnabledPlaneScalarNode = (sceneRecord.Entries[0].Content.Value as IPlainScalarNode);
                    var isEnabled = isEnabledPlaneScalarNode?.Text.GetText().Equals("1");
                    if (path == null || !isEnabled.HasValue)
                        continue;
                    
                    cacheItem.Scenes.SceneNamesFromBuildSettings.Add(path);
                    if (!isEnabled.Value)
                    {
                        cacheItem.Scenes.DisabledSceneNamesFromBuildSettings.Add(path);
                    }
                }
            }
        }
    }
}