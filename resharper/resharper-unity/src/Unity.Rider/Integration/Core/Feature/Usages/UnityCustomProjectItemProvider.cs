using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.Rider.Backend.Features.Usages;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Usages
{
    public abstract class UnityCustomProjectItemProvider : ProjectItemRule.ICustomProjectItemProvider
    {
        public abstract RdUsageGroupTextAndIcon? GetUsageGroup(IOccurrence occurrence, ProjectItemKind kind, bool takeParent); 
        
        [CanBeNull]
        protected RelativePath GetPresentablePath(VirtualFileSystemPath assetFile, VirtualFileSystemPath solutionFolder)
        {
            // Get a path that is relative to Library/PackageCache. This will give us a root folder of the package
            // name, e.g. com.unity.foo@1.0.0. We add a prefix of <External Packages>, which is the same as the solution
            // folder group. If we are grouping by solution folder, we show a nice icon for it. If we're not grouping
            // by solution folder, we still show it as the root folder name
            
            // todo: support local packages
            var packageCacheFolder = solutionFolder.Combine("Library/PackageCache");
            if (packageCacheFolder.IsPrefixOf(assetFile))
                return assetFile.MakeRelativeTo(packageCacheFolder);
            if (solutionFolder.IsPrefixOf(assetFile))
                return assetFile.MakeRelativeTo(solutionFolder);
            return null;
        }
    }
}