using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    // Provide a custom icon for our supported file types. If not, fall back to the language specific "YAML" icon.
    // Make sure we have a higher priority than other implementations so that we come first
    [ProjectModelElementPresenter(200)]
    public class UnityYamlProjectModelElementPresenter : IProjectModelElementPresenter
    {
        public IconId GetIcon(IProjectModelElement projectModelElement)
        {
            if (projectModelElement is IProjectFile projectFile && projectFile.LanguageType.Is<YamlProjectFileType>())
            {
                var location = projectFile.Location;
                if (location.IsAsset())
                    return UnityFileTypeThemedIcons.FileUnityAsset.Id;
                if (location.IsScene())
                    return UnityFileTypeThemedIcons.FileUnity.Id;
                if (location.IsPrefab())
                    return UnityFileTypeThemedIcons.FileUnityPrefab.Id;
                if (location.IsMeta())
                    return UnityFileTypeThemedIcons.FileUnityMeta.Id;
            }

            return null;
        }

        public string GetPresentableLocation(IProjectModelElement projectModelElement) => null;
    }
}