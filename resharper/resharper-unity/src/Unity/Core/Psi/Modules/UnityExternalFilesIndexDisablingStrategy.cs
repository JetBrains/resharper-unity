using System;
using System.Collections.Generic;
using JetBrains.Application.Notifications;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.ReSharper.Plugins.Unity.Resources;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [SolutionComponent]
    public class UnityExternalFilesIndexDisablingStrategy
    {
        // This key now applies to any external file being indexed (e.g. YAML based assets, JSON based asmdef). Maintain
        // name for compatibility with existing projects
        private const string HeuristicDisabledPersistentPropertyKey = "ShouldApplyYamlHugeFileHeuristic";

        private const long AssetFileSizeThreshold = 250L * (1024 * 1024); // 250 MB
        private const long TotalFileSizeThreshold = 4_000L * (1024 * 1024); // 4 GB

        private readonly Lifetime myLifetime;
        private readonly SolutionCaches mySolutionCaches;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly UserNotifications myUserNotifications;
        private readonly ILogger myLogger;
        private readonly bool myAllowRunHeuristic;
        private readonly bool myHeuristicDisabledForSolution;

        public UnityExternalFilesIndexDisablingStrategy(Lifetime lifetime,
                                                        SolutionCaches solutionCaches,
                                                        IApplicationWideContextBoundSettingStore settingsStore,
                                                        AssetIndexingSupport assetIndexingSupport,
                                                        UserNotifications userNotifications,
                                                        ILogger logger)
        {
            myLifetime = lifetime;
            mySolutionCaches = solutionCaches;
            myAssetIndexingSupport = assetIndexingSupport;
            myUserNotifications = userNotifications;
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

        private void NotifyAssetIndexingDisabled()
        {
            myUserNotifications.CreateNotification(myLifetime,
                NotificationSeverity.WARNING,
                Strings.DisabledIndexingOfUnityAssets_Text,
                Strings.DueToTheSizeOfTheProjectIndexingOfUnity_Text,
                closeAfterExecution: true,
                executed: new UserNotificationCommand(Strings.TurnOnAnyway_Text,
                    () => myAssetIndexingSupport.IsEnabled.SetValue(true)));
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

        private static (long maxFileSize, long totalFileSize) CalculateFileSizes(
            List<UnityExternalFilesModuleProcessor.ExternalFile> externalFiles)
        {
            var maxFileSize = 0L;
            var totalFileSize = 0L;

            foreach (var externalFile in externalFiles)
            {
                // Don't count large binary asset files. We wouldn't index them anyway
                if (externalFile.FileSystemData.FileLength > AssetFileSizeThreshold && externalFile.Path.IsAsset() &&
                    !externalFile.Path.SniffYamlHeader())
                {
                    continue;
                }

                totalFileSize += externalFile.FileSystemData.FileLength;
                maxFileSize = Math.Max(maxFileSize, externalFile.FileSystemData.FileLength);
            }

            return (maxFileSize, totalFileSize);
        }
    }
}