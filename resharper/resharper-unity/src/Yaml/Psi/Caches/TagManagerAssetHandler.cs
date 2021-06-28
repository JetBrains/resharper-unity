using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Text;
using JetBrains.Util;

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
            if (!(sourceFile.GetDominantPsiFile<YamlLanguage>() is IYamlFile file))
                return;

            var document = file.GetFirstMatchingUnityObjectDocument("TagManager");

            // An empty array will be an IFlowSequenceNode with no elements, otherwise we'll get a block sequence
            var tagsArrayNode = document.GetUnityObjectPropertyValue<INode>("tags");
            if (tagsArrayNode == null)
            {
                myLogger.Error("tagsArray == null");
                return;
            }

            if (tagsArrayNode is IBlockSequenceNode tagsArray)
            {
                foreach (var s in tagsArray.EntriesEnumerable)
                {
                    var text = s.Value.GetPlainScalarText();
                    if (!text.IsNullOrEmpty())
                        cacheItem.Tags.Add(text);
                }
            }

            var layersArray = document.GetUnityObjectPropertyValue<IBlockSequenceNode>("layers");
            if (layersArray != null)
            {
                foreach (var s in layersArray.EntriesEnumerable)
                {
                    var text = s.Value.GetPlainScalarText();
                    if (!text.IsNullOrEmpty())
                        cacheItem.Layers.Add(text);
                }
            }
            else
            {
                // Older versions of Unity (not sure when, but pre-5.6) have a v1 format, where each layer is stored as
                // a named property - "Builtin Layer 0:" to "Builtin Layer 7:" and "User Layer 8:" to "User Layer 31:"
                var objectProperties = document.GetUnityObjectProperties();
                if (objectProperties == null)
                {
                    myLogger.Error("Cannot find v1 or v2 layers");
                    return;
                }

                foreach (var entry in objectProperties.EntriesEnumerable)
                {
                    var propertyName = entry.Key.GetPlainScalarTextBuffer();
                    if (propertyName != null &&
                        (propertyName.StartsWith("Builtin Layer") || propertyName.StartsWith("User Layer")))
                    {
                        var text = entry.Content?.Value?.GetPlainScalarText();
                        if (!text.IsNullOrEmpty())
                            cacheItem.Layers.Add(text);
                    }
                }
            }
        }
    }
}