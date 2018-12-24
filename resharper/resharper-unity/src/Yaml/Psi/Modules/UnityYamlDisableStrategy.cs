using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class UnityYamlDisableStrategy
    {
        // stats
        public readonly List<long> PrefabSizes = new List<long>();
        public readonly List<long> SceneSizes = new List<long>();
        public readonly List<long> AssetSizes = new List<long>();
        public long TotalSize = 0;

        private readonly IProperty<bool> myEnabled;
        private readonly AssetSerializationMode myAssetSerializationMode;
        protected readonly IProperty<bool> YamlParsingEnabled;
        private const long YamlFileSizeThreshold = 40 * (1024 * 1024); // 40 MB
        private const long YamlFileTotalSizeThreshold = 700 * (1024 * 1024); // 700 MB

        public UnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, 
            AssetSerializationMode assetSerializationMode, UnityYamlEnabled unityYamlEnabled)
        {
            myAssetSerializationMode = assetSerializationMode;
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            myEnabled = boundStore.GetValueProperty(lifetime, (UnitySettings s) => s.EnableYamlHeuristic);
            YamlParsingEnabled = unityYamlEnabled.YamlParsingEnabled;

        }

        private bool IsYamlParsingAvailable()
        {
            return myAssetSerializationMode.IsForceText && YamlParsingEnabled.Value;
        }
        
        public void Run(FileSystemPath solutionDirectory)
        {
            if (myEnabled.Value)
            {
                if (!IsYamlParsingAvailable())
                {
                    YamlParsingEnabled.Value = false;
                } 
                else if (IsAnyFilePreventYamlParsing(solutionDirectory) || TotalSize > YamlFileTotalSizeThreshold)
                {
                    YamlParsingEnabled.Value = false;
                    CreateNotification();
                }
                myEnabled.Value = false;
            }
        }

        protected virtual void CreateNotification()
        {
            
        }

        private bool IsAnyFilePreventYamlParsing(FileSystemPath solutionDirectory)
        {
            // TODO : replace | with || after statistics will be collected
            return IsAnyFileInDirectoryPreventYamlParsing(solutionDirectory, "Assets") |
                   IsAnyFileInDirectoryPreventYamlParsing(solutionDirectory, "Packages") |
                   IsAnyFileInDirectoryPreventYamlParsing(solutionDirectory, "ProjectSettings");
        }

        private bool IsAnyFileInDirectoryPreventYamlParsing(FileSystemPath solutionDirectory, string relativePath)
        {
            var directory = solutionDirectory.Combine(relativePath);
            var files = directory.GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories,
                FileSystemPathInternStrategy.TRY_GET_INTERNED_BUT_DO_NOT_INTERN);

            var preventParsing = false;
            foreach (var file in files)
            {
                if (!file.IsAsset())
                    continue;

                if (IsYamlFilePreventParsing(file))
                {
                    // TODO : break cycle here after statistics will be collected
                    preventParsing = true;
                }
            }

            return preventParsing;
        }
        
        private bool IsYamlFilePreventParsing(FileSystemPath path)
        {
            HandleStatistics(path);
            var length = path.GetFileLength();
            if (length > YamlFileSizeThreshold)
            {
                if (path.ExtensionNoDot.Equals("asset", StringComparison.OrdinalIgnoreCase) && !path.IsYaml())
                {
                    return false;
                }
                
                TotalSize += length;
                return true;
            }

            TotalSize += length;
            return false;
        }

        private void HandleStatistics(FileSystemPath path)
        {
            var extension = path.ExtensionNoDot;
            if (extension.Equals("asset", StringComparison.OrdinalIgnoreCase))
            {
                AssetSizes.Add(path.GetFileLength());
            }
            
            if (extension.Equals("prefab", StringComparison.OrdinalIgnoreCase))
            {
                PrefabSizes.Add(path.GetFileLength());
            }
            
            if (extension.Equals("unity", StringComparison.OrdinalIgnoreCase))
            {
                SceneSizes.Add(path.GetFileLength());
            }
        }
    }
}