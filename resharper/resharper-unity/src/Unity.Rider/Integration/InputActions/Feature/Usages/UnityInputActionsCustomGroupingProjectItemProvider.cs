using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Search;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Usages;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Backend.Features.Usages;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.InputActions.Feature.Usages
{
    [SolutionComponent]
    public class UnityInputActionsCustomGroupingProjectItemProvider : UnityCustomProjectItemProvider
    {
        private readonly ISolution mySolution;
        private readonly ProjectModelIcons myProjectModelIcons;
        private readonly IconHost myIconHost;

        public UnityInputActionsCustomGroupingProjectItemProvider(ISolution solution, ProjectModelIcons projectModelIcons,
                                                            IconHost iconHost)
        {
            mySolution = solution;
            myProjectModelIcons = projectModelIcons;
            myIconHost = iconHost;
        }

        public override RdUsageGroupTextAndIcon? GetUsageGroup(IOccurrence occurrence, ProjectItemKind kind, bool takeParent)
        {
            if (occurrence is UnityInputActionsTextOccurence inputActionsTextOccurence)
            {
                switch (kind)
                {
                    case ProjectItemKind.PROJECT:
                        return new RdUsageGroupTextAndIcon("", null);
                    case ProjectItemKind.PHYSICAL_FILE:
                        var filePath = GetPresentablePath(inputActionsTextOccurence.SourceFile.GetLocation(), mySolution.SolutionDirectory);
                        if (filePath != null)
                            return new RdUsageGroupTextAndIcon(filePath.FullPath, myIconHost.Transform(GetIcon()));
                        break;

                    case ProjectItemKind.PHYSICAL_DIRECTORY:
                        var directoryPath = GetPresentablePath(inputActionsTextOccurence.SourceFile.GetLocation().Directory, mySolution.SolutionDirectory);
                        if (directoryPath != null)
                            return new RdUsageGroupTextAndIcon(directoryPath.FullPath, myIconHost.Transform(myProjectModelIcons.DirectoryIcon));
                        break;
                }
            }

            return null;
        }

        private IconId GetIcon()
        {
            return UnityFileTypeThemedIcons.UsageInputActions.Id;
        }
    }
}