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
    public class InputManagerAssetHandler : IProjectSettingsAssetHandler
    {
        private readonly ILogger myLogger;

        public InputManagerAssetHandler(ILogger logger)
        {
            myLogger = logger;
        }
        
        public bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.Equals("InputManager.asset");
        }

        public void Build(IPsiSourceFile sourceFile, ProjectSettingsCacheItem cacheItem)
        {
            {
                var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IYamlFile;
                var inputs = GetCollection(file, "InputManager", "m_Axes");

                if (inputs == null)
                {
                    myLogger.Error("inputs != null");
                    return;
                }
                
                if (inputs is IBlockSequenceNode node)
                {
                    foreach (var s in node.Entries)
                    {
                        var input = s.Value;
                        var inputRecord = input as IBlockMappingNode;
                        if (inputRecord == null)
                            continue;

                        var name = inputRecord.Entries.FirstOrDefault(t => t.Key.GetText().Equals(UnityYamlConstants.NameProperty))?.Content.Value.GetPlainScalarText();
                        
                        if (!name.IsNullOrEmpty())
                            cacheItem.Inputs.Add(name);
                    }
                }
            }
        }
    }
}