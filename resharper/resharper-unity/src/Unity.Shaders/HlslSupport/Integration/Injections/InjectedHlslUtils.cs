#nullable enable
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;

public static class InjectedHlslUtils
{
    public static void InvalidatePsiForInjectedLocation(ISolution solution, CppFileLocation location)
    {
        foreach (var sourceFile in location.GetAllSourceFiles(solution))
        {
            var psiFiles = sourceFile.GetPsiServices().Files;
            if (psiFiles.PsiFilesCache.TryGetCachedPsiFile(sourceFile, sourceFile.PrimaryPsiLanguage) is not { PsiFile: { } psiFile })
                continue;
            var range = location.RootRange;
            if (psiFile.FindNodeAt(TreeTextRange.FromLength(new TreeOffset(range.StartOffset), range.Length)) is not {} node)
                continue;
            psiFiles.PsiChanged(node, PsiChangedElementType.InvalidateCached);
        }    
    }
}