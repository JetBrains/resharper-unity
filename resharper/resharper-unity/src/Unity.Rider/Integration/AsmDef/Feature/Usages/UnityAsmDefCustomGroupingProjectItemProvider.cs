using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.Rider.Backend.Features.Usages;
using JetBrains.Rider.Backend.Platform.Icons;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.AsmDef.Feature.Usages
{
    [SolutionComponent]
    public class UnityAsmDefCustomGroupingProjectItemProvider : ProjectItemRule.ICustomProjectItemProvider
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

        public RdUsageGroupTextAndIcon? GetUsageGroup(IOccurrence occurrence, ProjectItemKind kind, bool takeParent)
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
                        var filePath = GetPackageRelativePath(sourceLocation);
                        if (filePath != null)
                        {
                            // TODO: Use a (new) specific .asmdef file icon
                            return new RdUsageGroupTextAndIcon(ExternalPackagesPrefix + filePath.FullPath,
                                myIconHost.Transform(PsiJavaScriptThemedIcons.Json.Id));
                        }
                        break;

                    case ProjectItemKind.PHYSICAL_DIRECTORY:
                        var directoryPath = GetPackageRelativePath(sourceLocation.Directory);
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
                            directoryPath = GetPackageRelativePath(sourceLocation);
                            if (directoryPath != null)
                            {
                                return new RdUsageGroupTextAndIcon(
                                    ExternalPackagesPrefix + directoryPath.Components.FirstOrEmpty,
                                    myIconHost.Transform(UnityFileTypeThemedIcons.FolderPackageReferenced.Id));
                            }
                        }
                        return null;
                }
            }

            return null;
        }

        [CanBeNull]
        private RelativePath GetPackageRelativePath(VirtualFileSystemPath assetFile)
        {
            // Get a path that is relative to Library/PackageCache. This will give us a root folder of the package
            // name, e.g. com.unity.foo@1.0.0. We add a prefix of <External Packages>, which is the same as the solution
            // folder group. If we are grouping by solution folder, we show a nice icon for it. If we're not grouping
            // by solution folder, we still show it as the root folder name
            var solutionFolder = mySolution.SolutionFile?.Location.Parent;
            var packageCacheFolder = solutionFolder?.Combine("Library/PackageCache");
            if (packageCacheFolder != null && packageCacheFolder.IsPrefixOf(assetFile))
                return assetFile.MakeRelativeTo(packageCacheFolder);
            if (solutionFolder != null && solutionFolder.IsPrefixOf(assetFile))
                return assetFile.MakeRelativeTo(solutionFolder);
            return null;
        }
    }
}