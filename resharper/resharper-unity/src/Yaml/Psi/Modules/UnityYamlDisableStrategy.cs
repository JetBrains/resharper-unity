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
        private long myTotalSize = 0;

        private readonly IProperty<bool> myApplied;
        private readonly AssetSerializationMode myAssetSerializationMode;
        protected readonly IProperty<bool> YamlParsingEnabled;
        private const long YamlFileSizeThreshold = 40 * (1024 * 1024); // 40 MB
        private const long YamlFileTotalSizeThreshold = 700 * (1024 * 1024); // 700 MB

        public UnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, 
            AssetSerializationMode assetSerializationMode, UnityYamlSupport unityYamlEnabled)
        {
            myAssetSerializationMode = assetSerializationMode;
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            myApplied = boundStore.GetValueProperty(lifetime, (UnitySettings s) => s.IsYamlHeuristicApplied);
            YamlParsingEnabled = unityYamlEnabled.IsYamlParsingEnabled;

        }

        private bool IsYamlParsingAvailable()
        {
            return myAssetSerializationMode.IsForceText && YamlParsingEnabled.Value;
        }
        
        public void Run(FileSystemPath solutionDirectory)
        {
            if (!myApplied.Value)
            {
                if (!IsYamlParsingAvailable())
                {
                    YamlParsingEnabled.Value = false;
                } 
                else if (IsAnyFilePreventYamlParsing(solutionDirectory) || myTotalSize > YamlFileTotalSizeThreshold)
                {
                    YamlParsingEnabled.Value = false;
                    CreateNotification();
                }
                myApplied.Value = true;
            }
        }

        protected virtual void CreateNotification()
        {
            
        }

        private bool IsAnyFilePreventYamlParsing(FileSystemPath solutionDirectory)
        {
            return IsAnyFileInDirectoryPreventYamlParsing(solutionDirectory, "Assets") ||
                   IsAnyFileInDirectoryPreventYamlParsing(solutionDirectory, "Packages") ||
                   IsAnyFileInDirectoryPreventYamlParsing(solutionDirectory, "ProjectSettings");
        }

        private bool IsAnyFileInDirectoryPreventYamlParsing(FileSystemPath solutionDirectory, string relativePath)
        {
            var directory = solutionDirectory.Combine(relativePath);
            var files = directory.GetChildFiles("*", PathSearchFlags.RecurseIntoSubdirectories,
                FileSystemPathInternStrategy.TRY_GET_INTERNED_BUT_DO_NOT_INTERN);

            foreach (var file in files)
            {
                if (!file.IsAsset())
                    continue;

                if (IsYamlFilePreventParsing(file))
                    return true;
            }

            return false;
        }
        
        private bool IsYamlFilePreventParsing(FileSystemPath path)
        {
            var length = path.GetFileLength();
            if (length > YamlFileSizeThreshold)
            {
                if (path.ExtensionNoDot.Equals("asset", StringComparison.OrdinalIgnoreCase) && !path.IsYaml())
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