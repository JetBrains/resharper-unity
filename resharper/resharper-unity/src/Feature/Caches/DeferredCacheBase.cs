using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    public abstract class DeferredCacheBase<T> : IDeferredCache
    {
        
        public bool UpToDate(IPsiSourceFile sourceFile)
        {
            throw new System.NotImplementedException();
        }

        public void Merge(IPsiSourceFile psiSourceFile, object build)
        {
            throw new System.NotImplementedException();
        }

        public object Build(in Lifetime lifetime, IPsiSourceFile psiSourceFile)
        {
            throw new System.NotImplementedException();
        }

        public void Drop(IPsiSourceFile psiSourceFile)
        {
            throw new System.NotImplementedException();
        }
        
    }
}