using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util.Collections;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class EditorBuildSettingsAssetHandler : IProjectSettingsAssetHandler
    {
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("EditorBuildSettings.asset");
        }

        public ProjectSettingsCacheItem Build(IPsiSourceFile sourceFile)
        {
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            var scenesArray =
                (((file?.Documents[0].Body.BlockNode as IBlockMappingNode)?.Entries[0].Value as IBlockMappingNode)
                    ?.Entries[2].Value as IBlockSequenceNode)?.Entries;
            if (scenesArray == null)
                return null;

            var scenePaths = new CountingSet<string>();
            foreach (var s in scenesArray)
            {
                var scene = s.Value;
                var path = (scene as IBlockMappingNode)?.Entries[1].Value.GetText();
                if (path != null)
                {
                    scenePaths.Add(path);
                }
            }

            if (scenePaths.Count == 0)
                return null;

            return new ProjectSettingsCacheItem(scenePaths, new CountingSet<string>(), new CountingSet<string>(),
                new CountingSet<string>());
        }
    }
}