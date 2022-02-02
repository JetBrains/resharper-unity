using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

#nullable enable

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
        private readonly ILogger myLogger;
        private readonly bool myAllowRunHeuristic;
        private readonly bool myHeuristicDisabledForSolution;

        public UnityExternalFilesIndexDisablingStrategy(SolutionCaches solutionCaches,
                                                        IApplicationWideContextBoundSettingStore settingsStore,
                                                        AssetIndexingSupport assetIndexingSupport,
                                                        ILogger logger)
        {
            mySolutionCaches = solutionCaches;
            myAssetIndexingSupport = assetIndexingSupport;
            myLogger = logger;

            myAllowRunHeuristic = settingsStore.BoundSettingsStore
                .GetValue((UnitySettings s) => s.EnableAssetIndexingPerformanceHeuristic);

            myHeuristicDisabledForSolution = IsHeuristicDisabledForSolution();
        }

        public void Run(List<UnityExternalFilesModuleProcessor.ExternalFile> externalFiles)
        {
            myLogger.Verbose("Checking automatic asset index disable heuristics for {0} external asset files",
                externalFiles.Count);

            if (externalFiles.IsEmpty()) return;

            if (!myAllowRunHeuristic)
            {
                myLogger.Verbose(
                    "'Automatically disable asset index' option disabled in settings. Not running heuristic. Asset indexing: {0}",
                    myAssetIndexingSupport.IsEnabled.Value ? "enabled" : "disabled");
                return;
            }

            if (!myAssetIndexingSupport.IsEnabled.Value)
            {
                myLogger.Verbose("Asset indexing disabled. No need to examine external file counts/sizes");
                return;
            }

            if (myHeuristicDisabledForSolution)
            {
                myLogger.Verbose("Asset indexing heuristic previously disabled. Not running heuristic again. Asset indexing: {0}",
                    myAssetIndexingSupport.IsEnabled.Value ? "enabled" : "disabled");
                return;
            }

            var (maxFileSize, totalFileSize) = CalculateFileSizes(externalFiles);

            var disableIndexing = false;
            if (maxFileSize > AssetFileSizeThreshold)
            {
                // There's no point in logging the file path of the largest file. If we're interested in that sort of
                // thing, run in TRACE mode and you'll get more details
                myLogger.Verbose("Max external file size meets criteria to disable indexing: {0:n0} bytes", maxFileSize);
                disableIndexing = true;
            }

            if (totalFileSize > TotalFileSizeThreshold)
            {
                myLogger.Verbose("Total external file size meets criteria to disable indexing: {0:n0} bytes", totalFileSize);
                disableIndexing = true;
            }

            if (disableIndexing)
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

        private static (ulong maxFileSize, ulong totalFileSize) CalculateFileSizes(
            List<UnityExternalFilesModuleProcessor.ExternalFile> externalFiles)
        {
            var maxFileSize = 0UL;
            var totalFileSize = 0UL;

            foreach (var externalFile in externalFiles)
            {
                // Don't count large binary asset files. We wouldn't index them anyway
                if (externalFile.Length > AssetFileSizeThreshold && externalFile.Path.IsAsset() &&
                    !externalFile.Path.SniffYamlHeader())
                {
                    continue;
                }

                totalFileSize += externalFile.Length;
                maxFileSize = Math.Max(maxFileSize, externalFile.Length);
            }

            return (maxFileSize, totalFileSize);
        }
    }
}