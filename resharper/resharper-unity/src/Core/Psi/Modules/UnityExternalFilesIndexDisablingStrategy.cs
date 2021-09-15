using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [SolutionComponent]
    public class UnityExternalFilesIndexDisablingStrategy
    {
        // This key now applies to any external file being indexed (e.g. YAML based assets, JSON based asmdef). Maintain
        // name for compatibility with existing projects
        private const string HeuristicDisabledPersistentPropertyKey = "ShouldApplyYamlHugeFileHeuristic";

        private const ulong AssetFileSizeThreshold = 250L * (1024 * 1024); // 250 MB
        private const ulong TotalFileSizeThreshold = 4_000L * (1024 * 1024); // 4 GB

        private readonly SolutionCaches mySolutionCaches;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly bool myAllowRunHeuristic;
        private readonly bool myHeuristicDisabledForSolution;
        private ulong myTotalSize;

        public UnityExternalFilesIndexDisablingStrategy(SolutionCaches solutionCaches,
                                                        IApplicationWideContextBoundSettingStore settingsStore,
                                                        AssetIndexingSupport assetIndexingSupport)
        {
            mySolutionCaches = solutionCaches;
            myAssetIndexingSupport = assetIndexingSupport;

            myAllowRunHeuristic = settingsStore.BoundSettingsStore
                .GetValue((UnitySettings s) => s.EnableAssetIndexingPerformanceHeuristic);

            myHeuristicDisabledForSolution = IsHeuristicDisabledForSolution();
        }

        public void Run(List<VirtualDirectoryEntryData> directoryEntries)
        {
            if (!myAllowRunHeuristic || myHeuristicDisabledForSolution || !myAssetIndexingSupport.IsEnabled.Value)
                return;

            if (DoesAnyFilePreventIndexing(directoryEntries) || myTotalSize > TotalFileSizeThreshold)
            {
                // If the project is too big, disable asset indexing. This unchecks the "index text assets" checkbox
                // in settings, saved at the solution level (more accurately .sln.DotSettings.user). It can be
                // re-enabled by checking the checkbox in settings.
                myAssetIndexingSupport.IsEnabled.Value = false;

                // Disable the heuristic in the solution cache. We do not reset this value if indexing is re-enabled, so
                // the heuristic remains disabled, so we don't automatically disable indexing again.
                // (If the user resets caches, then the heuristic kicks in again)
                DisableHeuristicForSolution();

                NotifyAssetIndexingDisabled();
            }
        }

        protected virtual void NotifyAssetIndexingDisabled()
        {
        }

        private bool IsHeuristicDisabledForSolution()
        {
            // Check to see if the heuristic is disabled for this solution. Usually set (to false) if the heuristic has
            // disabled indexing, then we've turned it back on again. If we re-enable indexing without disabling the
            // heuristics, then the heuristic would simply disable indexing again.
            return mySolutionCaches.PersistentProperties.TryGetValue(HeuristicDisabledPersistentPropertyKey, out var result) &&
                   !bool.Parse(result);
        }

        private void DisableHeuristicForSolution()
        {
            mySolutionCaches.PersistentProperties[HeuristicDisabledPersistentPropertyKey] = false.ToString();
        }

        private bool DoesAnyFilePreventIndexing(List<VirtualDirectoryEntryData> directoryEntries)
        {
            foreach (var directoryEntry in directoryEntries)
            {
                if (DoesAnyFilePreventIndexing(directoryEntry))
                    return true;
            }

            return false;
        }

        private bool DoesAnyFilePreventIndexing(VirtualDirectoryEntryData path)
        {
            var length = path.Length;
            if (length > AssetFileSizeThreshold)
            {
                if (path.RelativePath.IsAsset() && !path.GetAbsolutePath().SniffYamlHeader())
                {
                    return false;
                }

                myTotalSize += length;
                return true;
            }

            myTotalSize += length;
            return false;
        }
    }
}