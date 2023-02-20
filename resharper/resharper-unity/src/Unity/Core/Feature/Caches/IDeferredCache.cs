using JetBrains.DocumentManagers.impl;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches
{
    public interface IDeferredCache
    {
        bool IsApplicable(IPsiSourceFile sourceFile);
        bool UpToDate(IPsiSourceFile sourceFile);
        void Merge(IPsiSourceFile psiSourceFile, object build);
        object Build(IPsiSourceFile psiSourceFile);
        void Drop(IPsiSourceFile psiSourceFile);
        void Load();
        void MergeLoadedData();
        void Invalidate();
        void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange change);
    }
}