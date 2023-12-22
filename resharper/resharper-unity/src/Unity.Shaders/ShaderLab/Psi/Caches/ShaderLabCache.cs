#nullable enable
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Common.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches
{
    [PsiComponent]
    public class ShaderLabCache : SimplePsiSourceFileCacheWithLocalCache<ShaderLabCacheItem, IDeclaredElement>
    {
        private readonly ISolution mySolution;
        
        public ShaderLabCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager, ISolution solution) : base(lifetime, locks, persistentIndexManager, ShaderLabCacheItem.Marshaller, "Unity::Shaders::ShaderLabCacheUpdated")
        {
            mySolution = solution;
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile) => base.IsApplicable(sourceFile) && sourceFile.LanguageType.Is<ShaderLabProjectFileType>();

        public override object? Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (sourceFile.GetDominantPsiFile<ShaderLabLanguage>() is not ShaderLabFile file)
                return null;
            
            var name = file.DeclaredName;
            return name != SharedImplUtil.MISSING_DECLARATION_NAME ? new ShaderLabCacheItem(name, file.GetTreeStartOffset().Offset) : null;
        }

        protected override IDeclaredElement BuildLocal(IPsiSourceFile sourceFile, ShaderLabCacheItem newPart) =>
            new ShaderDeclaredElement(newPart.Name, sourceFile, newPart.DeclarationOffset);

        /// <summary>Returns table of all shaders declared with ShaderLab.</summary>
        public ISymbolTable GetShaderSymbolTable()
        {
            var values = LocalCacheValues;
            if (values.Count == 0)
                return EmptySymbolTable.INSTANCE;
            var psiServices = mySolution.GetComponent<IPsiServices>();
            return new DeclaredElementsSymbolTable<IDeclaredElement>(psiServices, values);
        }
    }
}