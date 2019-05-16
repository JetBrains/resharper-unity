using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class UnityYamlDisableStrategy
    {
        public const string SolutionCachesId = "ShouldApplyYamlHugeFileHeuristic";
        private const ulong AssetFileSizeThreshold = 40 * (1024 * 1024); // 40 MB
        private const ulong TotalFileSizeThreshold = 700 * (1024 * 1024); // 700 MB

        private readonly bool myShouldRunHeuristic;
        private readonly SolutionCaches mySolutionCaches;
        private readonly UnityYamlSupport myUnityYamlSupport;

        private ulong myTotalSize;

        public UnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, SolutionCaches solutionCaches, ISettingsStore settingsStore, UnityYamlSupport unityYamlSupport)
        {
            mySolutionCaches = solutionCaches;
            myUnityYamlSupport = unityYamlSupport;
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            var oldValue = boundStore.GetValue((UnitySettings s) => s.ShouldApplyYamlHugeFileHeuristic);
            if (!oldValue)
                mySolutionCaches.PersistentProperties[SolutionCachesId] = false.ToString();

            if (mySolutionCaches.PersistentProperties.TryGetValue(SolutionCachesId, out var result))
            {
                myShouldRunHeuristic = Boolean.Parse(result);
            }
            else
            {
                myShouldRunHeuristic = true;
            }
        }

        public void Run(List<DirectoryEntryData> directoryEntries)
        {
            if (myShouldRunHeuristic && myUnityYamlSupport.IsUnityYamlParsingEnabled.Value)
            {
                if (IsAnyFilePreventYamlParsing(directoryEntries) || myTotalSize > TotalFileSizeThreshold)
                {
                    mySolutionCaches.PersistentProperties[SolutionCachesId] = false.ToString();
                    myUnityYamlSupport.IsUnityYamlParsingEnabled.Value = false;
                    NotifyYamlParsingDisabled();
                }
            }
        }

        protected virtual void NotifyYamlParsingDisabled()
        {
        }

        private bool IsAnyFilePreventYamlParsing(List<DirectoryEntryData> directoryEntries)
        {
            foreach (var directoryEntry in directoryEntries)
            {
                if (IsYamlFilePreventParsing(directoryEntry))
                    return true;
            }

            return false;
        }

        private bool IsYamlFilePreventParsing(DirectoryEntryData path)
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