#nullable enable
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [ShellComponent]
    public class UnityExternalProjectFileTypes
    {
        public static readonly Key<ExternalModuleFileFlags> ExternalModuleFileFlagsKey = new("ExternalModuleFileFlags");
        
        private readonly Dictionary<StringSlice, ExternalFileInfo> myFileInfos = new();
        
        public UnityExternalProjectFileTypes(UserDataPerProjectFileType userDataPerProjectFileType)
        {
            foreach (var (projectFileType, properties) in userDataPerProjectFileType)
            {
                if (properties.TryGetValue(ExternalModuleFileFlagsKey, out var mode) && mode != ExternalModuleFileFlags.None)
                {
                    foreach (var extension in projectFileType.Extensions) 
                        myFileInfos.Add(extension, new ExternalFileInfo(projectFileType, mode));
                }
            }
        }

        public bool TryGetFileInfo(IPath path, out ExternalFileInfo info)
        {
            var name = path.Name;
            var index = name.LastIndexOf('.');
            if (index >= 0)
                return myFileInfos.TryGetValue(name.Slice(index, name.Length - index), out info);
            info = default;
            return false;
        }

        public bool ShouldBeIndexed(IPath path, bool assetIndexingEnabled) => TryGetFileInfo(path, out var info) && (assetIndexingEnabled ? info.FileFlags.HasFlag(ExternalModuleFileFlags.IndexWhenAssetsEnabled) : info.FileFlags.Has(ExternalModuleFileFlags.IndexWhenAssetsDisabled));

        public bool ShouldBeTreatedAsNonGenerated(IPath path) => TryGetFileInfo(path, out var info) && info.FileFlags.HasFlag(ExternalModuleFileFlags.TreatAsNonGenerated);

        public readonly struct ExternalFileInfo
        {
            public readonly ProjectFileType ProjectFileType;
            public readonly ExternalModuleFileFlags FileFlags;

            public ExternalFileInfo(ProjectFileType projectFileType, ExternalModuleFileFlags fileFlags)
            {
                ProjectFileType = projectFileType;
                FileFlags = fileFlags;
            }
        }
    }
}