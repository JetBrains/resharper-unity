using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections
{
    [Language(typeof(ShaderLabLanguage))]
    public class InjectedHlslCachingService : IInjectCachingService
    {
        public IEnumerable<IFile> GetInjectedFiles(Type injectedLanguageType, IFile dominantFile,
            IReadOnlyCollection<IInjectedPsiProvider> providersToBuild,
            Func<IFile, IReadOnlyList<IFile>> injectedFilesCalculator)
        {
            // Default implementation ignores white space changes and does not invalidate cache, for shader lab whitespace changes are significant, because
            // we use absolute file offsets for cpp injects, thus whitespace change leads to cpp inject invalidation, but default cache continues return invalid value

            return injectedFilesCalculator(dominantFile);
        }
    }
}