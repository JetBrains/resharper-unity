using System.Collections.Generic;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Model2.Assemblies.Impl;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Core;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Pdb;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Pdb.Cache;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Symbols;
using JetBrains.Symbols.SourceLinks;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.ExternalSources
{
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    [ZoneMarker(typeof(ExternalSourcesZone))]
    public class UnityProjectFolderPdbServiceImpl : PdbServiceImpl, IUnityProjectFolderLazyComponent
    {
        private readonly UnitySolutionTracker mySolutionTracker;

        public UnityProjectFolderPdbServiceImpl(Lifetime lifetime, ISolution solution, SrcSrvSourcesCache srcSrvSourcesCache,
            SourceLinkOrEmbeddedSourcesCache sourceLinkOrEmbeddedSourcesCache, ISourcesDownloader sourcesDownloader,
            PdbCache pdbCache, IExternalSourcesMappingChangeManager externalSourcesMappingChangeManager,
            AssemblyCollection assemblyCollection, ExternalSourcesActivation externalSourcesActivation,
            ISettingsStore settingsStore, ILazy<ISourceLinkNotifications> sourceLinkNotifications,
            ILazy<ISourceLinkCredentialManager> sourceLinkCredentialManager,
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
            if (!mySolutionTracker.IsUnityProject.Value || !fsp.IsAbsolute ||
                !SolutionDirectory.IsPrefixOf(fsp)) yield break;
            
            // on mac path in the pdb looks like ./Library/PackageCache/com.unity.ide.rider@4d374c7eb6db/Rider/Editor/Discovery.cs
            var unityPdbRelPath = "." + FileSystemDefinition.GetPathSeparator(InteractionContext.Local) +
                           fsp.MakeRelativeTo(SolutionDirectory).PathWithCurrentPlatformSeparators();
            yield return (FileSystemPath.TryParse(unityPdbRelPath), unityPdbRelPath);
        }
    }
}