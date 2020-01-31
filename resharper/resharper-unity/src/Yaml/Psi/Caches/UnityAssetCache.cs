using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class UnityAssetCache : DeferredCacheBase<int>
    {
        public UnityAssetCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, UnsafeMarshallers.IntMarshaller)
        {
        }

        public override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.PsiModule is UnityExternalFilesPsiModule;
        }

        protected override void MergeData(IPsiSourceFile sourceFile, int build)
        {
        }

        public override object Build(in Lifetime lifetime, IPsiSourceFile psiSourceFile)
        {
            return 5;
        }

        public override void DropData(IPsiSourceFile sourceFile, int data)
        {
        }

        public override void MergeLoadedData()
        {
        }

        public override void InvalidateData()
        {
        }
    }
}