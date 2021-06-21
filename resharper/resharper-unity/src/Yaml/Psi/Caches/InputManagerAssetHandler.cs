using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class InputManagerAssetHandler : IProjectSettingsAssetHandler
    {
        private readonly ILogger myLogger;

        public InputManagerAssetHandler(ILogger logger)
        {
            myLogger = logger;
        }

        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("InputManager.asset")  && sourceFile.GetLocation().SniffYamlHeader();
        }

        public void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem)
        {
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            var inputs = file.GetUnityObjectPropertyValue<IBlockSequenceNode>("InputManager", "m_Axes");
            if (inputs == null)
            {
                myLogger.Error("inputs == null");
                return;
            }

            foreach (var s in inputs.Entries)
            {
                var input = s.Value as IBlockMappingNode;
                if (input == null)
                    continue;

                var name = input.GetMapEntryPlainScalarText(UnityYamlConstants.NameProperty);
                if (!name.IsNullOrEmpty())
                    cacheItem.Inputs.Add(name);
            }
        }
    }
}