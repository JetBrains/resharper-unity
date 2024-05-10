using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Usages;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Backend.Features.Usages;
using JetBrains.Rider.Backend.Platform.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.AsmDef.Feature.Usages
{
    [SolutionComponent(Instantiation.DemandAnyThread)]
    public class UnityAsmDefCustomGroupingProjectItemProvider : UnityCustomProjectItemProvider
    {
        private const string ExternalPackages = "<External Packages>";
        private const string ExternalPackagesPrefix = ExternalPackages + "\\";

        private readonly ISolution mySolution;
        private readonly ProjectModelIcons myProjectModelIcons;
        private readonly IconHost myIconHost;

        public UnityAsmDefCustomGroupingProjectItemProvider(ISolution solution, ProjectModelIcons projectModelIcons,
                                                            IconHost iconHost)
        {
            mySolution = solution;
            myProjectModelIcons = projectModelIcons;
            myIconHost = iconHost;
        }

        public override RdUsageGroupTextAndIcon? GetUsageGroup(IOccurrence occurrence, ProjectItemKind kind, bool takeParent)
        {
            if (occurrence is ReferenceOccurrence referenceOccurrence &&
                referenceOccurrence.Kinds.Contains(AsmDefOccurrenceKindProvider.AssemblyDefinitionReference) &&
                referenceOccurrence.PrimaryReference is AsmDefNameReference reference)
            {
                var sourceLocation = reference.GetTreeNode().GetSourceFile()?.GetLocation();
                if (sourceLocation == null || sourceLocation.IsEmpty)
                    return null;

                // If the .asmdef belongs to a real project, let the default grouping handle it
                var isProjectItem = mySolution.FindProjectItemsByLocation(sourceLocation)
                    .Any(pi => !pi.IsMiscProjectItem());
                if (isProjectItem)
                    return null;

                switch (kind)
                {
                    case ProjectItemKind.PHYSICAL_FILE:
                        var filePath = GetPresentablePath(sourceLocation, mySolution.SolutionDirectory);
                        if (filePath != null)
                        {
                            return new RdUsageGroupTextAndIcon(ExternalPackagesPrefix + filePath.FullPath,
                                myIconHost.Transform(UnityFileTypeThemedIcons.AsmdefPackage.Id));
                        }
                        break;

                    case ProjectItemKind.PHYSICAL_DIRECTORY:
                        var directoryPath = GetPresentablePath(sourceLocation.Directory, mySolution.SolutionDirectory);
                        if (directoryPath != null)
                        {
                            return new RdUsageGroupTextAndIcon(ExternalPackagesPrefix + directoryPath.FullPath,
                                myIconHost.Transform(myProjectModelIcons.DirectoryIcon));
                        }
                        break;

                    case ProjectItemKind.PROJECT:
                        if (takeParent)
                        {
                            // If takeParent is true, it means it's a solution folder(s). We'll invent an external
                            // packages folder to group our external files
                            return new RdUsageGroupTextAndIcon(ExternalPackages,
                                myIconHost.Transform(UnityObjectTypeThemedIcons.UnityPackages.Id));
                        }
                        else
                        {
                            // Group by package
                            directoryPath = GetPresentablePath(sourceLocation, mySolution.SolutionDirectory);
                            if (directoryPath != null)
                            {
                                return new RdUsageGroupTextAndIcon(
                                    ExternalPackagesPrefix + directoryPath.Components.FirstOrEmpty,
                                    myIconHost.Transform(UnityFileTypeThemedIcons.FolderPackageReferenced.Id));
                            }
                        }
                        return new RdUsageGroupTextAndIcon(string.Empty, myIconHost.Transform(UnityFileTypeThemedIcons.FileUnity.Id));
                }
            }

            return null;
        }

    }
}