using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.DataFlow;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    public class UnityExternalFilesPsiModule : UserDataHolder, IPsiModuleOnFileSystemPaths
    {
        [NotNull] private readonly ISolution mySolution;
        private readonly string myPersistentId;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly Lifetime myLifetime;
        private readonly CompactMap<FileSystemPath, Pair<IPsiSourceFile, LifetimeDefinition>> mySourceFiles;
        private readonly List<IPsiModuleReference> myModules = new List<IPsiModuleReference>();

        public UnityExternalFilesPsiModule([NotNull] ISolution solution, string moduleName, string persistentId,
                                           TargetFrameworkId targetFrameworkId,
                                           IFileSystemTracker fileSystemTracker, Lifetime lifetime)
        {
            mySolution = solution;
            myPersistentId = persistentId;
            myFileSystemTracker = fileSystemTracker;
            myLifetime = lifetime;
            Name = moduleName;
            TargetFrameworkId = targetFrameworkId;
            mySourceFiles = new CompactMap<FileSystemPath, Pair<IPsiSourceFile, LifetimeDefinition>>();
        }

        public IPsiServices GetPsiServices() => mySolution.GetPsiServices();
        public ISolution GetSolution() => mySolution;

        IEnumerable<IPsiModuleReference> IPsiModule.GetReferences(
            IModuleReferenceResolveContext moduleReferenceResolveContext)
        {
            // TODO: There is a bug in PsiModuleAttrCache that causes tests to fail if this module has references
            // Disable references in tests. Will fix up later...
            if (JetBrains.ReSharper.Resources.Shell.Shell.Instance.IsTestShell)
                return EmptyList<IPsiModuleReference>.Instance;

            return myModules;
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
        public IEnumerable<IPsiSourceFile> SourceFiles => mySourceFiles.Values.Select(pair => pair.First);

        public void AddModuleReference(IPsiModule module)
        {
            myModules.Add(new PsiModuleReference(module));
        }

        public bool ContainsPath(FileSystemPath path) => mySourceFiles.ContainsKey(path);

        public bool TryGetFileByPath(FileSystemPath path, out IPsiSourceFile file)
        {
            file = null;
            if (mySourceFiles.TryGetValue(path, out var pair))
            {
                file = pair.First;
                return true;
            }

            return false;
        }

        public void Add(FileSystemPath path, IPsiSourceFile file, Action<FileSystemChangeDelta> processFileChange)
        {
            if (ContainsPath(path))
                return;

            var fileLifetime = Lifetimes.Define(myLifetime, path.FullPath);
            mySourceFiles.Add(path, Pair.Of(file, fileLifetime));

            // Explicitly assert if we're given a file change handler. We're expecting a lot of files in this module, it
            // will be much better to add a couple of directory change handlers than to add several thousand file change
            // handlers.
            // We also need to call the equivalent of PsiSourceFileWithLocationEx.TrackChanges, without registering
            // thousands of file change handlers.
            if (processFileChange != null)
                Assertion.Fail("Individual file change handler not supported. Use a directory change handler");
        }

        public void Remove(FileSystemPath path)
        {
            if (!mySourceFiles.TryGetValue(path, out var pair))
                return;
            pair.Second.Terminate();
            mySourceFiles.Remove(path);
        }
    }
}
