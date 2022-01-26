using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Backend.Features.Usages;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Yaml.Feature.Usages
{
    [SolutionComponent]
    public class UnityAssetRiderUsageGroupingProjectItemProvider : ProjectItemRule.ICustomProjectItemProvider
    {
        private readonly ISolution mySolution;
        private readonly ProjectModelIcons myProjectModelIcons;
        private readonly IconHost myIconHost;

        public UnityAssetRiderUsageGroupingProjectItemProvider(ISolution solution, ProjectModelIcons projectModelIcons, IconHost iconHost)
        {
            mySolution = solution;
            myProjectModelIcons = projectModelIcons;
            myIconHost = iconHost;
        }

        public RdUsageGroupTextAndIcon? GetUsageGroup(IOccurrence occurrence, ProjectItemKind kind, bool takeParent)
        {
            if (occurrence is UnityAssetOccurrence unityAssetOccurrence)
            {
                switch (kind)
                {
                    case ProjectItemKind.PROJECT:
                        return new RdUsageGroupTextAndIcon("", null);
                    case ProjectItemKind.PHYSICAL_FILE:
                        var filePath = GetPresentablePath(unityAssetOccurrence.SourceFile.GetLocation());
                        if (filePath != null)
                            return new RdUsageGroupTextAndIcon(filePath, myIconHost.Transform(GetIcon(unityAssetOccurrence.SourceFile)));
                        break;

                    case ProjectItemKind.PHYSICAL_DIRECTORY:
                        var directoryPath = GetPresentablePath(unityAssetOccurrence.SourceFile.GetLocation().Directory);
                        if (directoryPath != null)
                            return new RdUsageGroupTextAndIcon(directoryPath, myIconHost.Transform(myProjectModelIcons.DirectoryIcon));
                        break;
                }
            }

            return null;
        }

        private static IconId GetIcon(IPsiSourceFile sourceFile)
        {
            var location = sourceFile.GetLocation();
            if (location.IsAsset())
                return UnityFileTypeThemedIcons.FileUnityAsset.Id;
            if (location.IsScene())
                return UnityFileTypeThemedIcons.FileUnity.Id;
            if (location.IsPrefab())
                return UnityFileTypeThemedIcons.FileUnityPrefab.Id;
            if (location.IsMeta())
                return UnityFileTypeThemedIcons.FileUnityMeta.Id;
            if (location.IsController())
                return UnityFileTypeThemedIcons.FileAnimatorController.Id;
            return location.IsAnim()
                ? UnityFileTypeThemedIcons.FileAnimationClip.Id
                : UnityFileTypeThemedIcons.FileUnity.Id;
        }

        private string GetPresentablePath(VirtualFileSystemPath assetFile)
        {
            var solutionFolder = mySolution.SolutionFile?.Location.Parent;
            if (solutionFolder != null && solutionFolder.IsPrefixOf(assetFile))
                return assetFile.MakeRelativeTo(solutionFolder).FullPath;
            return null;
        }
    }
}