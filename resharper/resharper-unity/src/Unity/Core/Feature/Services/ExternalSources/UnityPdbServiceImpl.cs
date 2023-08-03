using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Model2.Assemblies.Impl;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Core;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Pdb;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Pdb.Cache;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Symbols;
using JetBrains.Symbols.SourceLinks;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.ExternalSources
{
    [SolutionComponent]
    public class UnityPdbServiceImpl : PdbServiceImpl
    {
        private readonly UnitySolutionTracker mySolutionTracker;

        public UnityPdbServiceImpl(Lifetime lifetime, ISolution solution, SrcSrvSourcesCache srcSrvSourcesCache,
            SourceLinkOrEmbeddedSourcesCache sourceLinkOrEmbeddedSourcesCache, ISourcesDownloader sourcesDownloader,
            PdbCache pdbCache, IExternalSourcesMappingChangeManager externalSourcesMappingChangeManager,
            AssemblyCollection assemblyCollection, ExternalSourcesActivation externalSourcesActivation,
            ISettingsStore settingsStore, ILazyComponent<ISourceLinkNotifications> sourceLinkNotifications,
            ILazyComponent<ISourceLinkCredentialManager> sourceLinkCredentialManager,
            UnitySolutionTracker solutionTracker) : base(lifetime, solution,
            srcSrvSourcesCache, sourceLinkOrEmbeddedSourcesCache, sourcesDownloader, pdbCache,
            externalSourcesMappingChangeManager, assemblyCollection, externalSourcesActivation, settingsStore,
            sourceLinkNotifications, sourceLinkCredentialManager)
        {
            mySolutionTracker = solutionTracker;
        }

        /// <summary>
        /// Provides paths from the base implementation and a path relative to the Unity project root.
        /// </summary>
        /// <remarks>
        /// Starting from Unity 2022, relative paths are used in their assemblies, so an additional relative path is provided.
        /// </remarks>
        public override IEnumerable<(FileSystemPath path, string url)> GetFilePathsWithFolderSubstitution(
            FileSystemPath fsp)
        {
            var paths = base.GetFilePathsWithFolderSubstitution(fsp);
            foreach (var path in paths) yield return path;
            if (!mySolutionTracker.IsUnityGeneratedProject.Value || !fsp.IsAbsolute ||
                !SolutionDirectory.IsPrefixOf(fsp)) yield break;
            var fullPath = "." + FileSystemDefinition.GetPathSeparator(InteractionContext.Local) +
                           fsp.MakeRelativeTo(SolutionDirectory).FullPath;
            yield return (FileSystemPath.TryParse(fullPath), fullPath);
        }
    }
}