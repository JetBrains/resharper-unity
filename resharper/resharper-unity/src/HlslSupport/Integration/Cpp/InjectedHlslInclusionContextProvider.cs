using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.VCXProj;
using JetBrains.ReSharper.Feature.Services.Cpp.Injections;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport.Caches;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Cpp
{
    [Language(typeof(ShaderLabLanguage))]
    public class InjectedHlslInclusionContextProvider : IInclusionContextProvider
    {
        public CppInclusionContextResult CreateInclusionContextResult(CppGlobalSymbolCache cache,
            CppFileLocation rootFile,
            FileProcessingOptions options, long cacheVersion, Lifetime lifetime)
        {
            var locationTracker = cache.Solution.GetComponent<InjectedHlslFileLocationTracker>();
            if (!locationTracker.IsValid(rootFile))
            {
                return CppInclusionContextResult.Fail(CppInclusionContextResult.Status.UNSUITABLE_PROJECT_FILE);
            }


            var properties = new CppCompilationProperties()
            {
                LanguageKind = CppLanguageKind.HLSL, ClrSupport = VCXCompileAsManagedOptions.ManagedNotSet,
            };

            properties.PredefinedMacros.Add(CppPPDefineSymbol.ParsePredefinedMacro("SHADER_API_D3D11"));
            var cgIncludeFolder =
                CgIncludeDirectoryTracker.GetCgIncludeFolderPath(cache.Solution.GetComponent<UnityVersion>());
            if (!cgIncludeFolder.IsEmpty)
            {
                properties.IncludePaths.Add(cgIncludeFolder);
                properties.IncludePaths.Add(cache.Solution.SolutionDirectory);
                properties.IncludePaths.Add(cache.Solution.SolutionDirectory.Combine("Library"));
            }

            var shaderCache = cache.Solution.GetComponent<InjectedHlslFileLocationTracker>();

            // TODO 1) is cache ready? what will happen under document transaction? check for bad moment?
            // TODO 2) what will happen under psi transaction? include in cache could be out-of date. Try use include quickfix when cginclude is after cgprogram where QF is used
            var includeLocation = shaderCache.GetIncludes(rootFile);

            return InjectInclusionContextProviderUtil.CreateInclusionContextResult(cache, rootFile,
                includeLocation, options, properties, null, cacheVersion, lifetime);
        }
    }
}