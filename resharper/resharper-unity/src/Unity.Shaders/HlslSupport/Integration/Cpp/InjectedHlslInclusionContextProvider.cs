#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.ReSharper.Psi.Cpp.Parsing.Preprocessor;
using JetBrains.ReSharper.Psi.Cpp.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Cpp
{
    [Language(typeof(ShaderLabLanguage))]
    public class InjectedHlslInclusionContextProvider : IInclusionContextProvider
    {
        public CppInclusionContextResult CreateInclusionContextResult(CppGlobalSymbolCache cache, CppFileLocation rootFile,
            FileProcessingOptions options, long cacheVersion, Lifetime lifetime)
        {
            var locationTracker = cache.Solution.GetComponent<InjectedHlslFileLocationTracker>();
            if (!locationTracker.IsValid(rootFile))
                return CppInclusionContextResult.Fail(CppInclusionContextResult.Status.UNSUITABLE_PROJECT_FILE);
            
            return CreateInclusionContextResult(cache, rootFile, options, null, cacheVersion, lifetime);
        }
        
        private static CppInclusionContextResult CreateInclusionContextResult(
            CppGlobalSymbolCache cache,
            CppFileLocation rootFile,
            FileProcessingOptions options,
            ISymbolScope? symbolScope,
            long cacheVersion,
            Lifetime lifetime)
        {
            var solution = cache.Solution;
            var sourceFile = rootFile.GetRandomSourceFile(solution);
            var randomProjectFile = sourceFile.ToProjectFile() ?? rootFile.GetRandomProjectFile(solution);
            
            // retrieve shader program info
            if (!solution.GetComponent<ShaderProgramCache>().TryGetOrReadUpToDateProgramInfo(sourceFile, rootFile, out var shaderProgramInfo))
                Assertion.Fail($"Shader program info is missing for {rootFile}");
         
            // create compilation properties
            var compilationPropertiesProvider = cache.Solution.GetComponent<UnityHlslCppCompilationPropertiesProvider>();
            var compilationProperties = compilationPropertiesProvider.GetShaderLabHlslCompilationProperties(solution, sourceFile.GetProject(), rootFile, shaderProgramInfo);
            
            // create inclusion context
            var languageDialect = CppProjectConfigurationUtil.GetLanguageDialect(compilationProperties);
            var inclusionContext = CppRootInclusionContext.Create(compilationProperties, randomProjectFile.GetProject(),
                randomProjectFile, cache, rootFile, options.File, languageDialect, 
                cacheVersion, options.AllowPendingActions, options.CollectPPUsages, lifetime, symbolScope);

            var directory = randomProjectFile.Location.Directory;
            var includes = solution.GetComponent<InjectedHlslFileLocationTracker>().GetIncludes(sourceFile, shaderProgramInfo);
            // TODO 1) is cache ready? what will happen under document transaction? check for bad moment?
            // TODO 2) what will happen under psi transaction? include in cache could be out-of date. Try use include quickfix when cginclude is after cgprogram where QF is used            
            inclusionContext.PushInclude(rootFile, directory, false);
            foreach (var includeLocation in includes)
            {
                if (includeLocation.IsValid() && !includeLocation.Equals(rootFile))
                {
                    cache.LookupAndProcessTableForFile(rootFile, includeLocation, options, inclusionContext, directory);
                }
            }
            inclusionContext.PopInclude(false);
            return CppInclusionContextResult.Ok(inclusionContext);
        }
    }
}