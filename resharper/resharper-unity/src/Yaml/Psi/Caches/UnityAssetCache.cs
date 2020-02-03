using System.Text;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class UnityAssetCache : DeferredCacheBase<UnityAssetData>
    {
        public UnityAssetCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, new UniversalMarshaller<UnityAssetData>(UnityAssetData.ReadDelegate, UnityAssetData.WriteDelegate))
        {
        }

        public override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.PsiModule is UnityExternalFilesPsiModule;
        }

        protected override void MergeData(IPsiSourceFile sourceFile, UnityAssetData build)
        {
        }

        public override object Build(in Lifetime lifetime, IPsiSourceFile psiSourceFile)
        {
            psiSourceFile.GetLocation().ReadTextStream((sr) =>
            {
                sr.ReadBlock()
            }, Encoding.UTF8);
            return 5;
        }

        public override void DropData(IPsiSourceFile sourceFile, UnityAssetData data)
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