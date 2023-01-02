using System;
using System.Collections.Generic;
using JetBrains.Application.Changes;
using JetBrains.DocumentModel;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.SourceGenerators;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [SolutionComponent]
    public class
        DotsElementsSuperTypeDeclaredInOtherPartSuppressorMock : DotsElementsSuperTypeDeclaredInOtherPartSuppressor
    {
        public override bool SuppressInspections(IClassLikeDeclaration classLikeDeclaration,
            IDeclaredType currentSuperInterfaceType,
            IPsiSourceFile otherSuperTypeSourceFile)
        {
            if (otherSuperTypeSourceFile.Name.Contains("Generated"))
                otherSuperTypeSourceFile = new SourceGeneratedFileMock(otherSuperTypeSourceFile);
            return base.SuppressInspections(classLikeDeclaration, currentSuperInterfaceType, otherSuperTypeSourceFile);
        }

        private class SourceGeneratedFileMock : ISourceGeneratorOutputFile
        {
            private readonly IPsiSourceFile mySourceFile;

            public SourceGeneratedFileMock(IPsiSourceFile sourceFile)
            {
                mySourceFile = sourceFile;
            }

            public T GetData<T>(Key<T> key) where T : class
            {
                return mySourceFile.GetData(key);
            }

            public void PutData<T>(Key<T> key, T value) where T : class
            {
                mySourceFile.PutData(key, value);
            }

            public T GetOrCreateDataUnderLock<T>(Key<T> key, Func<T> factory) where T : class
            {
                return mySourceFile.GetOrCreateDataUnderLock(key, factory);
            }

            public T GetOrCreateDataUnderLock<T, TState>(Key<T> key, TState state, Func<TState, T> factory)
                where T : class
            {
                return default;
            }
            

            public IEnumerable<KeyValuePair<object, object>> EnumerateData()
            {
                return default;
            }

            public IPsiModule PsiModule { get; }
            public IDocument Document { get; }
            public string Name { get; }
            public string DisplayName { get; }

            public bool IsValid()
            {
                return default;
            }

            public string GetPersistentID()
            {
                return default;
            }

            public ProjectFileType LanguageType { get; }
            public PsiLanguageType PrimaryPsiLanguage { get; }
            public IPsiSourceFileProperties Properties { get; }
            public IModuleReferenceResolveContext ResolveContext { get; }
            public IPsiSourceFileStorage PsiStorage { get; }
            public ModificationStamp? InMemoryModificationStamp { get; }
            public ModificationStamp? ExternalModificationStamp { get; }
            public DateTime LastWriteTimeUtc { get; }
            public VirtualFileSystemPath NavigationPath { get; }

            public void BindToProjectFile(IProjectFile projectFile)
            {
            }

            public void UnbindFromProjectFile(IProjectFile projectFile)
            {
            }

            public Guid CacheId { get; }
            public IDocument AssociatedEditorDocument { get; }
            public IDocument AssociatedEmbeddedSourceDocument { get; }
            public string RelativePath { get; }
            public int CodePage { get; set; }
            public string AnalyzerReferencePath { get; }

            public void BindToEmbeddedSourceProjectFile(IProjectFile projectFile)
            {
            }

            public void UnbindFromEmbeddedSourceProjectFile(IProjectFile projectFile)
            {
            }
        }
    }
}