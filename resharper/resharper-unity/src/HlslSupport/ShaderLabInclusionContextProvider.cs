using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.VCXProj;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport
{
    [Language(typeof(ShaderLabLanguage))]
    public class ShaderLabInclusionContextProvider : IInclusionContextProvider
    {
        public CppInclusionContextResult CreateInclusionContextResult(CppGlobalSymbolCache cache,
            CppFileLocation rootFile,
            FileProcessingOptions options, long cacheVersion, Lifetime lifetime)
        {
            var locationTracker = cache.Solution.GetComponent<ShaderLabCppFileLocationTracker>();
            if (!locationTracker.IsValid(rootFile))
            {
                return CppInclusionContextResult.Fail(CppInclusionContextResult.Status.UNSUITABLE_PROJECT_FILE);
            }


            var properties = new CppCompilationProperties()
            {
                LanguageKind = CppLanguageKind.HLSL, ClrSupport = VCXCompileAsManagedOptions.ManagedNotSet,
            };

            var cgIncludeFolder =
                CgIncludeDirectoryTracker.GetCgIncludeFolderPath(cache.Solution.GetComponent<UnityVersion>());
            if (cgIncludeFolder != null)
            {
                properties.IncludePaths.Add(cgIncludeFolder);
            }

            var shaderCache = cache.Solution.GetComponent<ShaderLabCppFileLocationTracker>();

            // TODO 1) is cache ready? what will happen under document transaction? check for bad moment?
            // TODO 2) what will happen under psi transaction? include in cache could be out-of date. Try use include quickfix when cginclude is after cgprogram where QF is used
            var includeLocation = shaderCache.GetCgIncludeLocation(rootFile.GetRandomSourceFile(cache.Solution));

            return InjectInclusionContextProviderUtil.CreateInclusionContextResult(cache, rootFile,
                new[] {includeLocation}, options, properties, null, cacheVersion, lifetime);
        }
    }
}