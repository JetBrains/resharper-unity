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

        private readonly IProperty<bool> ShouldBeApplied;
        private readonly AssetSerializationMode myAssetSerializationMode;
        protected readonly IProperty<bool> YamlParsingEnabled;
        private const long YamlFileSizeThreshold = 40 * (1024 * 1024); // 40 MB
        private const long YamlFileTotalSizeThreshold = 700 * (1024 * 1024); // 700 MB

        public UnityYamlDisableStrategy(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, 
            AssetSerializationMode assetSerializationMode, UnityYamlSupport unityYamlEnabled)
        {
            myAssetSerializationMode = assetSerializationMode;
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext()));
            ShouldBeApplied = boundStore.GetValueProperty(lifetime, (UnitySettings s) => s.ShouldBeAppliedYamlHeuristic);
            YamlParsingEnabled = unityYamlEnabled.IsYamlParsingEnabled;

        }

        public void Run(List<FileSystemPath> files)
        {
            if (ShouldBeApplied.Value)
            {
                if (IsAnyFilePreventYamlParsing(files) || myTotalSize > YamlFileTotalSizeThreshold)
                {
                    YamlParsingEnabled.Value = false;
                    CreateNotification();
                }
            }
        }

        protected virtual void CreateNotification()
        {
            
        }

        private bool IsAnyFilePreventYamlParsing(List<FileSystemPath> files)
        {
            foreach (var file in files)
            {
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