using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    public interface IDeferredCache
    {
        bool IsApplicable(IPsiSourceFile sourceFile);
        bool UpToDate(IPsiSourceFile sourceFile);
        void Merge(IPsiSourceFile psiSourceFile, object build);
        object Build(Lifetime lifetime, IPsiSourceFile psiSourceFile);
        void Drop(IPsiSourceFile psiSourceFile);
        void Load();
        void MergeLoadedData();
        void Invalidate();
    }
}