using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
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
    }
    
    // public interface IDeferredCache
    // {
    //     // WriteLock
    //     void Load();
    //     // WriteLock
    //     void MergeLoadedData();
    //     
    //     // ReadLock
    //     bool IsApplicable(IPsiSourceFile sourceFile);
    //     
    //     // ReadLock
    //     bool UpToDate(IPsiSourceFile sourceFile);
    //
    //
    //     // ReadLock
    //     IDeferredIndex GetIndex();
    //     // WriteLock
    //     void ReplaceIndex(IDeferredIndex newIndex);
    //     
    //     // ReadLock
    //     object CreateDataFor(IPsiSourceFile psiSourceFile);
    //     
    //     // No locks
    //     void PersistData(IPsiSourceFile psiSourceFile);
    //
    //     // WriteLock
    //     void Invalidate();
    //     
    // }
}