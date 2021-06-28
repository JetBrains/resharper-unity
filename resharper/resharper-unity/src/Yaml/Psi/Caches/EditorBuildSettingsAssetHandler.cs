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
            return sourceFile.Name.Equals("EditorBuildSettings.asset") && sourceFile.GetLocation().SniffYamlHeader();
        }

        public void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem)
        {
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            if (file == null)
                return;

            var scenesArrayNode = GetSceneCollection<INode>(file);
            switch (scenesArrayNode)
            {
                case null:
                    myLogger.Error("scenesArray == null");
                    return;
                case IBlockSequenceNode scenesArray:
                {
                    foreach (var s in scenesArray.Entries)
                    {
                        var scene = s.Value as IBlockMappingNode;
                        if (scene == null || scene.Entries.Count < 2)
                            continue;

                        var isEnabled = scene.GetMapEntryPlainScalarText("enabled")?.Equals("1");

                        var scenePath = scene.GetMapEntryPlainScalarText("path");
                        if (scenePath == null)
                            continue;

                        var path = GetUnityScenePathRepresentation(scenePath);
                        if (path == null || !isEnabled.HasValue)
                            continue;

                        cacheItem.Scenes.SceneNamesFromBuildSettings.Add(path);
                        if (!isEnabled.Value)
                            cacheItem.Scenes.DisabledSceneNamesFromBuildSettings.Add(path);
                    }

                    break;
                }
            }
        }
    }
}