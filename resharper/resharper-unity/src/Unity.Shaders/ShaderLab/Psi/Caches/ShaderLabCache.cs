#nullable enable
using JetBrains.Application.Parts;
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
using JetBrains.ReSharper.Psi.Files;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;

[PsiComponent(Instantiation.DemandAnyThreadSafe)]
public class ShaderLabCache(Lifetime lifetime, IShellLocks locks, IPersistentIndexManager persistentIndexManager)
    : SimplePsiSourceFileCacheWithLocalCache<ShaderLabCacheItem, IDeclaredElement>(lifetime, locks, persistentIndexManager, ShaderLabCacheItem.Marshaller, "Unity::Shaders::ShaderLabCacheUpdated")
{
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
}