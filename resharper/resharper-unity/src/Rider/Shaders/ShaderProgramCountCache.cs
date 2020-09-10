using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Shaders
{
    [SolutionComponent]
    public class ShaderProgramCountCache : SimpleICache<int>, IBuildMergeParticipant<IPsiSourceFile>
    {
        public ShaderProgramCountCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, UnsafeMarshallers.IntMarshaller)
        {
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.LanguageType.Is<ShaderLabProjectFileType>();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            return Build(sourceFile);
        }

        public object Build(IPsiSourceFile sourceFile)
        {
            var file = sourceFile.GetDominantPsiFile<ShaderLabLanguage>();
            return file?.Descendants<IProgramBlock>().Collect().Count;
        }
    }
}