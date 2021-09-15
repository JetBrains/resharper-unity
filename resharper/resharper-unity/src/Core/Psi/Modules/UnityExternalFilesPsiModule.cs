using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    public class UnityExternalFilesPsiModule : UserDataHolder, IPsiModuleOnFileSystemPaths, IResourceModule
    {
        [NotNull] private readonly ISolution mySolution;
        private readonly string myPersistentId;
        private readonly CompactMap<VirtualFileSystemPath, IPsiSourceFile> mySourceFiles;

        public UnityExternalFilesPsiModule([NotNull] ISolution solution, string moduleName, string persistentId,
                                           TargetFrameworkId targetFrameworkId)
        {
            mySolution = solution;
            myPersistentId = persistentId;
            Name = moduleName;
            TargetFrameworkId = targetFrameworkId;
            mySourceFiles = new CompactMap<VirtualFileSystemPath, IPsiSourceFile>();
        }

        public IPsiServices GetPsiServices() => mySolution.GetPsiServices();
        public ISolution GetSolution() => mySolution;

        IEnumerable<IPsiModuleReference> IPsiModule.GetReferences(
            IModuleReferenceResolveContext moduleReferenceResolveContext)
        {
            return EmptyList<IPsiModuleReference>.Instance;
        }

        public ICollection<PreProcessingDirective> GetAllDefines() => EmptyList<PreProcessingDirective>.InstanceList;
        public bool IsValid() => true;
        public string GetPersistentID() => myPersistentId;
        public string Name { get; }
        public string DisplayName => Name;
        public TargetFrameworkId TargetFrameworkId { get; }
        public PsiLanguageType PsiLanguage => UnknownLanguage.Instance;
        public ProjectFileType ProjectFileType => UnknownProjectFileType.Instance;
        public IModule ContainingProjectModule => mySolution.MiscFilesProject;
        public IEnumerable<IPsiSourceFile> SourceFiles => mySourceFiles.Values;

        public bool ContainsPath(VirtualFileSystemPath path) => mySourceFiles.ContainsKey(path);

        public bool TryGetFileByPath(VirtualFileSystemPath path, out IPsiSourceFile file)
        {
            file = null;
            return mySourceFiles.TryGetValue(path, out file);
        }

        public void Add(VirtualFileSystemPath path, IPsiSourceFile file, Action<FileSystemChangeDelta> processFileChange)
        {
            if (ContainsPath(path))
                return;

            mySourceFiles.Add(path, file);

            // Explicitly assert if we're given a file change handler. We're expecting a lot of files in this module, it
            // will be much better to add a couple of directory change handlers than to add several thousand file change
            // handlers.
            // We also need to call the equivalent of PsiSourceFileWithLocationEx.TrackChanges, without registering
            // thousands of file change handlers.
            if (processFileChange != null)
                Assertion.Fail("Individual file change handler not supported. Use a directory change handler");
        }

        public void Remove(VirtualFileSystemPath path)
        {
            mySourceFiles.Remove(path);
        }
    }
}
