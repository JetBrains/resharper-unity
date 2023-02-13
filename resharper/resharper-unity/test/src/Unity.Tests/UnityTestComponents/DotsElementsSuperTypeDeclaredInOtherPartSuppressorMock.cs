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
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent]
    public partial class DotsElementsSuperTypeDeclaredInOtherPartSuppressorMock : DotsElementsSuperTypeDeclaredInOtherPartSuppressor
    {
        public override bool SuppressInspections(IDeclaredType superType, IClassLikeDeclaration declaration,
            ITypeDeclaration otherPartDeclaration)
        {
            if (otherPartDeclaration.GetSourceFile()!.Name.Contains("Generated"))
                otherPartDeclaration = new ClassLikeDeclarationFromGeneratedFileMock(otherPartDeclaration);
            return base.SuppressInspections(superType, declaration, otherPartDeclaration);
        }

        private class SourceGeneratedFileMock : ISourceGeneratorOutputFile
        {
            private readonly IPsiSourceFile mySourceFile;

            public SourceGeneratedFileMock(IPsiSourceFile sourceFile)
            {
                mySourceFile = sourceFile;
            }

            public T? GetData<T>(Key<T> key) where T : class
            {
                return mySourceFile.GetData(key);
            }

            public void PutData<T>(Key<T> key, T? value) where T : class
            {
                mySourceFile.PutData(key, value!);
            }

            public T GetOrCreateDataUnderLock<T>(Key<T> key, Func<T> factory) where T : class
            {
                return mySourceFile.GetOrCreateDataUnderLock(key, factory);
            }

            public T GetOrCreateDataUnderLock<T, TState>(Key<T> key, TState state, Func<TState, T> factory)
                where T : class
            {
                return factory(state);
            }

            public IEnumerable<KeyValuePair<object, object>> EnumerateData()
            {
                return EmptyList<KeyValuePair<object, object>>.Enumerable;
            }

            public IPsiModule PsiModule => mySourceFile.PsiModule;
            public IDocument Document => mySourceFile.Document;
            public string Name => mySourceFile.Name;
            public string DisplayName => mySourceFile.DisplayName;

            public bool IsValid()
            {
                return default;
            }

            public string? GetPersistentID()
            {
                return default;
            }

            public ProjectFileType LanguageType => mySourceFile.LanguageType;
            public PsiLanguageType PrimaryPsiLanguage => mySourceFile.PrimaryPsiLanguage;
            public IPsiSourceFileProperties Properties => mySourceFile.Properties;
            public IModuleReferenceResolveContext ResolveContext => mySourceFile.ResolveContext;
            public IPsiSourceFileStorage PsiStorage => mySourceFile.PsiStorage;
            public ModificationStamp? InMemoryModificationStamp => mySourceFile.InMemoryModificationStamp;
            public ModificationStamp? ExternalModificationStamp => mySourceFile.ExternalModificationStamp;
            public DateTime LastWriteTimeUtc => mySourceFile.LastWriteTimeUtc;
            public VirtualFileSystemPath NavigationPath => mySourceFile.GetLocation();

            public void BindToProjectFile(IProjectFile projectFile)
            {
            }

            public void UnbindFromProjectFile(IProjectFile projectFile)
            {
            }

            public Guid CacheId { get; } = Guid.Empty;
            public IDocument? AssociatedEditorDocument { get; } = null;
            public IDocument? AssociatedEmbeddedSourceDocument { get; } = null;
            public string RelativePath { get; } = string.Empty;
            public int CodePage { get; set; }
            public string? AnalyzerReferencePath { get; } = null;

            public void BindToEmbeddedSourceProjectFile(IProjectFile projectFile)
            {
            }

            public void UnbindFromEmbeddedSourceProjectFile(IProjectFile projectFile)
            {
            }
        }
    }
}