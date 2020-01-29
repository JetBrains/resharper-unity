using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    public interface IDeferredCache
    {
        bool UpToDate(IPsiSourceFile sourceFile);
        void Merge(IPsiSourceFile psiSourceFile, object build);
        object Build(in Lifetime lifetime, IPsiSourceFile psiSourceFile);
        void Drop(IPsiSourceFile psiSourceFile);
        void Load();
        void MergeLoaded();
    }
}