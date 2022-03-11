using System;
using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    public class UnityExternalFilesPsiModule : UserDataHolder, IPsiModuleOnFileSystemPaths, IResourceModule
    {
        private readonly ISolution mySolution;
        private readonly string myPersistentId;
        private readonly JetHashSet<IPsiSourceFile> mySourceFiles;
        private readonly FileSystemPathTrie<IPsiSourceFile> mySourceFileTrie;

        public UnityExternalFilesPsiModule(ISolution solution, string moduleName, string persistentId,
                                           TargetFrameworkId targetFrameworkId)
        {
            mySolution = solution;
            myPersistentId = persistentId;

            Name = moduleName;
            TargetFrameworkId = targetFrameworkId;

            // Store the files as a flat set, and as a trie. We need the trie to be able to get all of the files under
            // a folder so that we can e.g. remove files when a package is removed/updated. However, we can't use the
            // trie as the backing for the SourceFiles property - it is not enumerable, and will generate a new list
            // of results on each call, and it's called frequently. When we have a very large project, this produces a
            // huge amount of memory traffic
            mySourceFiles = new JetHashSet<IPsiSourceFile>();
            mySourceFileTrie = new FileSystemPathTrie<IPsiSourceFile>(true);
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
        public PsiLanguageType PsiLanguage => UnknownLanguage.Instance!;
        public ProjectFileType ProjectFileType => UnknownProjectFileType.Instance!;
        public IModule ContainingProjectModule => mySolution.MiscFilesProject;
        public IEnumerable<IPsiSourceFile> SourceFiles => mySourceFiles;

        public bool ContainsFile(IPsiSourceFile sourceFile)
        {
            // This method is called by sourceFile.IsValid, which will be called A LOT for very large projects. Make
            // sure to keep it quick and allocation free. And don't call sourceFile.IsValid
            return sourceFile is UnityExternalPsiSourceFile && mySourceFiles.Contains(sourceFile);
        }

        public bool ContainsPath(VirtualFileSystemPath path) => mySourceFileTrie.Contains(path);

        public bool TryGetFileByPath(VirtualFileSystemPath path, out IPsiSourceFile file)
        {
            file = mySourceFileTrie[path];
            return file != null;
        }

        public void Add(VirtualFileSystemPath path, IPsiSourceFile file, Action<FileSystemChangeDelta>? processFileChange)
        {
            if (ContainsPath(path))
                return;

            mySourceFiles.Add(file);
            mySourceFileTrie.Add(path, file);

            // Explicitly assert if we're given a file change handler. We're expecting a lot of files in this module, it
            // will be much better to add a couple of directory change handlers than to add several thousand file change
            // handlers.
            // We also need to call the equivalent of PsiSourceFileWithLocationEx.TrackChanges, without registering
            // thousands of file change handlers.
            Assertion.Assert(processFileChange == null,
                "Individual file change handler not supported. Use a directory change handler");
        }

        public void Remove(VirtualFileSystemPath path)
        {
            var file = mySourceFileTrie.Find(path);
            if (file != null)
                mySourceFiles.Remove(file);
            mySourceFileTrie.Remove(path);
        }

        public IEnumerable<IPsiSourceFile> GetSourceFilesByRootFolder(VirtualFileSystemPath rootFolder) =>
            mySourceFileTrie.GetSubTreeData(rootFolder);
    }
}
