#nullable enable
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Collections;
using JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [ShellComponent]
    public class UnityIndexedExternalProjectFileTypeFilter
    {
        public static readonly Key<ExternalModuleIndexingMode> ExternalModuleIndexingModeKey = new("ExternalModuleIndexingMode");
        
        private readonly Dictionary<StringSlice, ExternalModuleIndexingMode> myIndexedFileExtensions = new();
        
        public UnityIndexedExternalProjectFileTypeFilter(UserDataPerProjectFileType userDataPerProjectFileType)
        {
            foreach (var (projectFileType, properties) in userDataPerProjectFileType)
            {
                if (properties.TryGetValue(ExternalModuleIndexingModeKey, out var mode) && mode != ExternalModuleIndexingMode.None)
                {
                    foreach (var extension in projectFileType.Extensions) 
                        myIndexedFileExtensions.Add(extension, mode);
                }
            }
        }

        public bool Accept(VirtualFileSystemPath path, bool assetIndexingEnabled)
        {
            var name = path.Name;
            var index = name.LastIndexOf('.');
            // in mapping we only have extensions with either Assets or Always mode
            return index >= 0 && myIndexedFileExtensions.TryGetValue(name.Slice(index, name.Length - index), out var mode) && (assetIndexingEnabled || mode == ExternalModuleIndexingMode.Always);
        }
    }
}