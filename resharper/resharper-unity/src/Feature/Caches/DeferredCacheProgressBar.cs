using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Caches
{
    [SolutionComponent]
    public class DeferredCacheProgressBar
    {
        protected IViewableProperty<IPsiSourceFile> CurrentFile;
        public DeferredCacheProgressBar(Lifetime lifetime, DeferredHelperCache cache)
        {
            CurrentFile = new ViewableProperty<IPsiSourceFile>(null);
        }

        public virtual void Start(Lifetime startLifetime)
        {
            // TODO 
        }

        public void SetCurrentProcessingFile(IPsiSourceFile sourceFile)
        {
            CurrentFile.Value = sourceFile;
        }
    }
}