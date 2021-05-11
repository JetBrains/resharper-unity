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
    public class TagManagerAssetHandler : IProjectSettingsAssetHandler
    {
        private readonly ILogger myLogger;

        public TagManagerAssetHandler(ILogger logger)
        {
            myLogger = logger;
        }
        
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("TagManager.asset") && sourceFile.GetLocation().SniffYamlHeader();
        }

        public void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem)
        {
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            var tagsArray = GetCollection(file, "TagManager", "tags");
            if (tagsArray == null)
            {
                myLogger.Error("tagsArray != null");
                return;
            }

            if (tagsArray is IBlockSequenceNode node)
            {
                foreach (var s in node.Entries)
                {
                    var text = (s.Value as IPlainScalarNode)?.Text.GetText();
                    if (!text.IsNullOrEmpty())
                        cacheItem.Tags.Add(text);
                }
            }
            
            var layersArray = GetCollection(file, "TagManager", "layers");
            if (layersArray == null)
            {
                myLogger.Error("layersArray != null");
                return;
            }
            
            if (layersArray is IBlockSequenceNode layersNode)
            {
                foreach (var s in layersNode.Entries)
                {
                    var text = (s.Value as IPlainScalarNode)?.Text.GetText();
                    if (!text.IsNullOrEmpty())
                        cacheItem.Layers.Add(text);
                }
            }
        }
    }
}